using TechTalk.SpecFlow;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using WindsurfProductAPI.Data;
using WindsurfProductAPI.Models;

namespace WindsurfProductAPI.Tests.StepDefinitions;

[Binding]
public class PaymentProcessingSteps : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private PaymentResponse? _currentPayment;
    private PaymentConfirmation? _paymentConfirmation;
    private PaymentIntent? _paymentStatus;
    private List<PaymentIntent>? _paymentHistory;
    private decimal _calculatedTotal;

    public PaymentProcessingSteps()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<ProductDbContext>(options =>
                    {
                        options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}");
                    });
                });
            });

        _client = _factory.CreateClient();
    }

    [When(@"I create a payment for product (.*) with quantity (.*) and email ""(.*)""")]
    public async Task WhenICreateAPaymentForProductWithQuantityAndEmail(int productId, int quantity, string email)
    {
        var paymentRequest = new PaymentRequest
        {
            ProductId = productId,
            Quantity = quantity,
            CustomerEmail = email,
            CustomerName = "Test Customer"
        };

        _response = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        
        if (_response.IsSuccessStatusCode)
        {
            _currentPayment = await _response.Content.ReadFromJsonAsync<PaymentResponse>();
        }
    }

    [Given(@"I have created a payment for product (.*) with quantity (.*)")]
    public async Task GivenIHaveCreatedAPaymentForProductWithQuantity(int productId, int quantity)
    {
        var paymentRequest = new PaymentRequest
        {
            ProductId = productId,
            Quantity = quantity,
            CustomerEmail = "test@example.com",
            CustomerName = "Test Customer"
        };

        var response = await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        _currentPayment = await response.Content.ReadFromJsonAsync<PaymentResponse>();
    }

    [Given(@"I want to purchase product (.*) with quantity (.*)")]
    public void GivenIWantToPurchaseProductWithQuantity(int productId, int quantity)
    {
        // Store for later use in the workflow
        ScenarioContext.Current["ProductId"] = productId;
        ScenarioContext.Current["Quantity"] = quantity;
    }

    [When(@"I create the payment with email ""(.*)""")]
    public async Task WhenICreateThePaymentWithEmail(string email)
    {
        var productId = (int)ScenarioContext.Current["ProductId"];
        var quantity = (int)ScenarioContext.Current["Quantity"];
        
        await WhenICreateAPaymentForProductWithQuantityAndEmail(productId, quantity, email);
    }

    [When(@"I calculate the price for product (.*) with quantity (.*)")]
    public async Task WhenICalculateThePriceForProductWithQuantity(int productId, int quantity)
    {
        _response = await _client.GetAsync($"/api/products/{productId}/calculate-price?quantity={quantity}");
        
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonDocument.Parse(content);
            _calculatedTotal = json.RootElement.GetProperty("totalAmount").GetDecimal();
        }
    }

    [When(@"I confirm the payment")]
    public async Task WhenIConfirmThePayment()
    {
        _response = await _client.PostAsync($"/api/payments/{_currentPayment!.PaymentIntentId}/confirm", null);
        
        if (_response.IsSuccessStatusCode)
        {
            _paymentConfirmation = await _response.Content.ReadFromJsonAsync<PaymentConfirmation>();
        }
    }

    [When(@"I cancel the payment")]
    public async Task WhenICancelThePayment()
    {
        _response = await _client.PostAsync($"/api/payments/{_currentPayment!.PaymentIntentId}/cancel", null);
        
        if (_response.IsSuccessStatusCode)
        {
            _paymentConfirmation = await _response.Content.ReadFromJsonAsync<PaymentConfirmation>();
        }
    }

    [When(@"I retrieve the payment status")]
    public async Task WhenIRetrieveThePaymentStatus()
    {
        _response = await _client.GetAsync($"/api/payments/{_currentPayment!.PaymentIntentId}");
        
        if (_response.IsSuccessStatusCode)
        {
            _paymentStatus = await _response.Content.ReadFromJsonAsync<PaymentIntent>();
        }
    }

    [Given(@"the following payments have been made:")]
    public async Task GivenTheFollowingPaymentsHaveBeenMade(Table table)
    {
        foreach (var row in table.Rows)
        {
            var paymentRequest = new PaymentRequest
            {
                ProductId = int.Parse(row["ProductId"]),
                Quantity = int.Parse(row["Quantity"]),
                CustomerEmail = row["CustomerEmail"]
            };

            await _client.PostAsJsonAsync("/api/payments/create", paymentRequest);
        }
    }

    [When(@"I request payment history for ""(.*)""")]
    public async Task WhenIRequestPaymentHistoryFor(string email)
    {
        _response = await _client.GetAsync($"/api/payments?customerEmail={email}");
        
        if (_response.IsSuccessStatusCode)
        {
            _paymentHistory = await _response.Content.ReadFromJsonAsync<List<PaymentIntent>>();
        }
    }

    [When(@"I request all payment history")]
    public async Task WhenIRequestAllPaymentHistory()
    {
        _response = await _client.GetAsync("/api/payments");
        
        if (_response.IsSuccessStatusCode)
        {
            _paymentHistory = await _response.Content.ReadFromJsonAsync<List<PaymentIntent>>();
        }
    }

    [Then(@"the payment should be created successfully")]
    public void ThenThePaymentShouldBeCreatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _currentPayment.Should().NotBeNull();
    }

    [Then(@"the payment intent ID should not be empty")]
    public void ThenThePaymentIntentIDShouldNotBeEmpty()
    {
        _currentPayment!.PaymentIntentId.Should().NotBeNullOrEmpty();
    }

    [Then(@"the payment amount should be (.*)")]
    public void ThenThePaymentAmountShouldBe(decimal expectedAmount)
    {
        _currentPayment!.Amount.Should().Be(expectedAmount);
    }

    [Then(@"the payment status should be valid")]
    public void ThenThePaymentStatusShouldBeValid()
    {
        _currentPayment!.Status.Should().NotBeNullOrEmpty();
    }

    [Then(@"the calculated total should be (.*)")]
    public void ThenTheCalculatedTotalShouldBe(decimal expectedTotal)
    {
        _calculatedTotal.Should().Be(expectedTotal);
    }

    [Then(@"the payment should be confirmed")]
    public void ThenThePaymentShouldBeConfirmed()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _paymentConfirmation.Should().NotBeNull();
    }

    [Then(@"the payment status should be ""(.*)""")]
    public void ThenThePaymentStatusShouldBe(string expectedStatus)
    {
        _paymentConfirmation!.Status.Should().Be(expectedStatus);
    }

    [Then(@"the payment should be canceled")]
    public void ThenThePaymentShouldBeCanceled()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _paymentConfirmation.Should().NotBeNull();
    }

    [Then(@"I should see the payment details")]
    public void ThenIShouldSeeThePaymentDetails()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _paymentStatus.Should().NotBeNull();
    }

    [Then(@"I should see (.*) payments")]
    public void ThenIShouldSeePayments(int count)
    {
        _paymentHistory.Should().NotBeNull();
        _paymentHistory!.Count.Should().Be(count);
    }

    [Then(@"I should see at least (.*) payments")]
    public void ThenIShouldSeeAtLeastPayments(int minCount)
    {
        _paymentHistory.Should().NotBeNull();
        _paymentHistory!.Count.Should().BeGreaterThanOrEqualTo(minCount);
    }

    [Then(@"all payments should be for ""(.*)""")]
    public void ThenAllPaymentsShouldBeFor(string email)
    {
        _paymentHistory.Should().AllSatisfy(p => p.CustomerEmail.Should().Be(email));
    }

    [Then(@"the payment creation should fail")]
    public void ThenThePaymentCreationShouldFail()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Then(@"I should receive an error message")]
    public void ThenIShouldReceiveAnErrorMessage()
    {
        var content = _response!.Content.ReadAsStringAsync().Result;
        content.Should().Contain("error");
    }

    [Then(@"the payment workflow should complete successfully")]
    public void ThenThePaymentWorkflowShouldCompleteSuccessfully()
    {
        _currentPayment.Should().NotBeNull();
        _paymentStatus.Should().NotBeNull();
        _paymentConfirmation.Should().NotBeNull();
    }

    [Then(@"the final status should be ""(.*)""")]
    public void ThenTheFinalStatusShouldBe(string expectedStatus)
    {
        _paymentConfirmation!.Status.Should().Be(expectedStatus);
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
