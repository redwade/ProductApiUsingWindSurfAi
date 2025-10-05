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
public class ShippingManagementSteps : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private HttpResponseMessage? _response;
    private List<ShippingRate>? _shippingRates;
    private ShipmentResponse? _currentShipment;
    private Shipment? _shipmentDetails;
    private List<TrackingUpdate>? _trackingUpdates;
    private List<Shipment>? _shipmentHistory;
    private decimal _calculatedCost;

    public ShippingManagementSteps()
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

    [When(@"I request shipping rates with the following details:")]
    public async Task WhenIRequestShippingRatesWithTheFollowingDetails(Table table)
    {
        var request = new ShippingRateRequest
        {
            FromAddress = new ShippingAddress
            {
                Name = "Warehouse",
                Street1 = "123 Main St",
                City = table.Rows[0]["Value"],
                State = table.Rows[1]["Value"],
                PostalCode = "12345",
                Country = "US"
            },
            ToAddress = new ShippingAddress
            {
                Name = "Customer",
                Street1 = "456 Oak Ave",
                City = table.Rows[2]["Value"],
                State = table.Rows[3]["Value"],
                PostalCode = "67890",
                Country = "US"
            },
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 10,
                Height = 8,
                Weight = decimal.Parse(table.Rows[4]["Value"])
            }
        };

        _response = await _client.PostAsJsonAsync("/api/shipping/rates", request);
        if (_response.IsSuccessStatusCode)
        {
            _shippingRates = await _response.Content.ReadFromJsonAsync<List<ShippingRate>>();
        }
    }

    [When(@"I create a shipment with the following details:")]
    public async Task WhenICreateAShipmentWithTheFollowingDetails(Table table)
    {
        var request = new CreateShipmentRequest
        {
            Provider = Enum.Parse<ShippingProvider>(table.Rows[0]["Value"]),
            Speed = Enum.Parse<ShippingSpeed>(table.Rows[1]["Value"]),
            FromAddress = new ShippingAddress
            {
                Name = "Warehouse",
                Street1 = "123 Main St",
                City = table.Rows[2]["Value"],
                State = table.Rows[3]["Value"],
                PostalCode = "12345",
                Country = "US"
            },
            ToAddress = new ShippingAddress
            {
                Name = "Customer",
                Street1 = "456 Oak Ave",
                City = table.Rows[4]["Value"],
                State = table.Rows[5]["Value"],
                PostalCode = "67890",
                Country = "US",
                Email = "customer@example.com"
            },
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 10,
                Height = 8,
                Weight = decimal.Parse(table.Rows[6]["Value"])
            }
        };

        _response = await _client.PostAsJsonAsync("/api/shipping/create", request);
        if (_response.IsSuccessStatusCode)
        {
            _currentShipment = await _response.Content.ReadFromJsonAsync<ShipmentResponse>();
        }
    }

    [Given(@"I have created a shipment with (.*)")]
    public async Task GivenIHaveCreatedAShipmentWith(string provider)
    {
        var request = CreateValidShipmentRequest(Enum.Parse<ShippingProvider>(provider));
        var response = await _client.PostAsJsonAsync("/api/shipping/create", request);
        _currentShipment = await response.Content.ReadFromJsonAsync<ShipmentResponse>();
    }

    [Given(@"I want to ship a package from warehouse to customer")]
    public void GivenIWantToShipAPackageFromWarehouseToCustomer()
    {
        // Setup for workflow scenario
        ScenarioContext.Current["WorkflowStarted"] = true;
    }

    [When(@"I track the shipment using the tracking number")]
    public async Task WhenITrackTheShipmentUsingTheTrackingNumber()
    {
        _response = await _client.GetAsync($"/api/shipping/track/{_currentShipment!.TrackingNumber}");
        if (_response.IsSuccessStatusCode)
        {
            _shipmentDetails = await _response.Content.ReadFromJsonAsync<Shipment>();
        }
    }

    [When(@"I request tracking updates")]
    public async Task WhenIRequestTrackingUpdates()
    {
        _response = await _client.GetAsync($"/api/shipping/track/{_currentShipment!.TrackingNumber}/updates");
        if (_response.IsSuccessStatusCode)
        {
            _trackingUpdates = await _response.Content.ReadFromJsonAsync<List<TrackingUpdate>>();
        }
    }

    [When(@"I cancel the shipment")]
    public async Task WhenICancelTheShipment()
    {
        _response = await _client.PostAsync($"/api/shipping/{_currentShipment!.ShipmentId}/cancel", null);
        if (_response.IsSuccessStatusCode)
        {
            _shipmentDetails = await _response.Content.ReadFromJsonAsync<Shipment>();
        }
    }

    [Given(@"the following shipments have been created:")]
    public async Task GivenTheFollowingShipmentsHaveBeenCreated(Table table)
    {
        foreach (var row in table.Rows)
        {
            var request = CreateValidShipmentRequest(
                Enum.Parse<ShippingProvider>(row["Provider"]),
                row["ToEmail"],
                decimal.Parse(row["Weight"])
            );
            await _client.PostAsJsonAsync("/api/shipping/create", request);
        }
    }

    [When(@"I request shipment history for ""(.*)""")]
    public async Task WhenIRequestShipmentHistoryFor(string email)
    {
        _response = await _client.GetAsync($"/api/shipping?customerEmail={email}");
        if (_response.IsSuccessStatusCode)
        {
            _shipmentHistory = await _response.Content.ReadFromJsonAsync<List<Shipment>>();
        }
    }

    [When(@"I request all shipment history")]
    public async Task WhenIRequestAllShipmentHistory()
    {
        _response = await _client.GetAsync("/api/shipping");
        if (_response.IsSuccessStatusCode)
        {
            _shipmentHistory = await _response.Content.ReadFromJsonAsync<List<Shipment>>();
        }
    }

    [When(@"I calculate shipping cost for (.*) with (.*) and (.*) pounds")]
    public async Task WhenICalculateShippingCostFor(string provider, string speed, decimal weight)
    {
        _response = await _client.GetAsync(
            $"/api/shipping/calculate-cost?provider={provider}&speed={speed}&weight={weight}");
        
        if (_response.IsSuccessStatusCode)
        {
            var content = await _response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonDocument.Parse(content);
            _calculatedCost = json.RootElement.GetProperty("cost").GetDecimal();
        }
    }

    [When(@"I create a shipment with (.*) and (.*) shipping")]
    public async Task WhenICreateAShipmentWithProviderAndSpeed(string provider, string speed)
    {
        var request = CreateValidShipmentRequest(
            Enum.Parse<ShippingProvider>(provider),
            "test@example.com",
            5,
            Enum.Parse<ShippingSpeed>(speed)
        );

        _response = await _client.PostAsJsonAsync("/api/shipping/create", request);
        if (_response.IsSuccessStatusCode)
        {
            _currentShipment = await _response.Content.ReadFromJsonAsync<ShipmentResponse>();
        }
    }

    [When(@"I select FedEx Express shipping")]
    public void WhenISelectFedExExpressShipping()
    {
        ScenarioContext.Current["SelectedProvider"] = ShippingProvider.FedEx;
        ScenarioContext.Current["SelectedSpeed"] = ShippingSpeed.Express;
    }

    [When(@"I create the shipment")]
    public async Task WhenICreateTheShipment()
    {
        var provider = (ShippingProvider)ScenarioContext.Current["SelectedProvider"];
        var speed = (ShippingSpeed)ScenarioContext.Current["SelectedSpeed"];
        
        var request = CreateValidShipmentRequest(provider, "customer@example.com", 5, speed);
        _response = await _client.PostAsJsonAsync("/api/shipping/create", request);
        _currentShipment = await _response.Content.ReadFromJsonAsync<ShipmentResponse>();
    }

    [When(@"I track the shipment")]
    public async Task WhenITrackTheShipment()
    {
        await WhenITrackTheShipmentUsingTheTrackingNumber();
    }

    [When(@"I request shipping rates with zero weight")]
    public async Task WhenIRequestShippingRatesWithZeroWeight()
    {
        var request = new ShippingRateRequest
        {
            FromAddress = CreateValidAddress("Warehouse"),
            ToAddress = CreateValidAddress("Customer"),
            Dimensions = new ShipmentDimensions { Length = 10, Width = 8, Height = 6, Weight = 0 }
        };

        _response = await _client.PostAsJsonAsync("/api/shipping/rates", request);
    }

    [Then(@"I should receive multiple shipping rates")]
    public void ThenIShouldReceiveMultipleShippingRates()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _shippingRates.Should().NotBeEmpty();
    }

    [Then(@"the rates should include (.*) options")]
    public void ThenTheRatesShouldIncludeOptions(string provider)
    {
        var providerEnum = Enum.Parse<ShippingProvider>(provider);
        _shippingRates.Should().Contain(r => r.Provider == providerEnum);
    }

    [Then(@"the rates should be sorted by cost")]
    public void ThenTheRatesShouldBeSortedByCost()
    {
        _shippingRates.Should().BeInAscendingOrder(r => r.Cost);
    }

    [Then(@"the shipment should be created successfully")]
    public void ThenTheShipmentShouldBeCreatedSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.Created);
        _currentShipment.Should().NotBeNull();
    }

    [Then(@"the tracking number should start with ""(.*)""")]
    public void ThenTheTrackingNumberShouldStartWith(string prefix)
    {
        _currentShipment!.TrackingNumber.Should().StartWith(prefix);
    }

    [Then(@"the shipment status should be ""(.*)""")]
    public void ThenTheShipmentStatusShouldBe(string status)
    {
        var statusEnum = Enum.Parse<ShipmentStatus>(status);
        if (_currentShipment != null)
        {
            _currentShipment.Status.Should().Be(statusEnum);
        }
        else if (_shipmentDetails != null)
        {
            _shipmentDetails.Status.Should().Be(statusEnum);
        }
    }

    [Then(@"the label URL should be provided")]
    public void ThenTheLabelURLShouldBeProvided()
    {
        _currentShipment!.LabelUrl.Should().NotBeNullOrEmpty();
    }

    [Then(@"I should see the shipment details")]
    public void ThenIShouldSeeTheShipmentDetails()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _shipmentDetails.Should().NotBeNull();
    }

    [Then(@"the shipment should have tracking updates")]
    public void ThenTheShipmentShouldHaveTrackingUpdates()
    {
        _shipmentDetails.Should().NotBeNull();
    }

    [Then(@"I should see at least (.*) tracking update")]
    public void ThenIShouldSeeAtLeastTrackingUpdate(int count)
    {
        _trackingUpdates.Should().NotBeNull();
        _trackingUpdates!.Count.Should().BeGreaterThanOrEqualTo(count);
    }

    [Then(@"each update should have a timestamp")]
    public void ThenEachUpdateShouldHaveATimestamp()
    {
        _trackingUpdates.Should().AllSatisfy(u => u.Timestamp.Should().BeAfter(DateTime.MinValue));
    }

    [Then(@"each update should have a location")]
    public void ThenEachUpdateShouldHaveALocation()
    {
        _trackingUpdates.Should().AllSatisfy(u => u.Location.Should().NotBeNullOrEmpty());
    }

    [Then(@"the shipment should be cancelled successfully")]
    public void ThenTheShipmentShouldBeCancelledSuccessfully()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.OK);
        _shipmentDetails.Should().NotBeNull();
    }

    [Then(@"I should see (.*) shipments")]
    public void ThenIShouldSeeShipments(int count)
    {
        _shipmentHistory.Should().NotBeNull();
        _shipmentHistory!.Count.Should().Be(count);
    }

    [Then(@"I should see at least (.*) shipments")]
    public void ThenIShouldSeeAtLeastShipments(int count)
    {
        _shipmentHistory.Should().NotBeNull();
        _shipmentHistory!.Count.Should().BeGreaterThanOrEqualTo(count);
    }

    [Then(@"all shipments should be for ""(.*)""")]
    public void ThenAllShipmentsShouldBeFor(string email)
    {
        _shipmentHistory.Should().AllSatisfy(s => s.ToEmail.Should().Be(email));
    }

    [Then(@"the calculated cost should be (.*)")]
    public void ThenTheCalculatedCostShouldBe(decimal expectedCost)
    {
        _calculatedCost.Should().Be(expectedCost);
    }

    [Then(@"the complete workflow should succeed")]
    public void ThenTheCompleteWorkflowShouldSucceed()
    {
        _currentShipment.Should().NotBeNull();
        _shipmentDetails.Should().NotBeNull();
    }

    [Then(@"I should have a valid tracking number")]
    public void ThenIShouldHaveAValidTrackingNumber()
    {
        _currentShipment!.TrackingNumber.Should().NotBeNullOrEmpty();
    }

    [Then(@"the request should fail")]
    public void ThenTheRequestShouldFail()
    {
        _response!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Then(@"I should receive a validation error")]
    [Then(@"I should receive an error message about delivered shipments")]
    public void ThenIShouldReceiveAValidationError()
    {
        var content = _response!.Content.ReadAsStringAsync().Result;
        content.Should().Contain("error");
    }

    private ShippingAddress CreateValidAddress(string name)
    {
        return new ShippingAddress
        {
            Name = name,
            Street1 = "123 Main St",
            City = "San Francisco",
            State = "CA",
            PostalCode = "94102",
            Country = "US",
            Email = $"{name.ToLower()}@example.com"
        };
    }

    private CreateShipmentRequest CreateValidShipmentRequest(
        ShippingProvider provider,
        string? email = null,
        decimal weight = 5,
        ShippingSpeed speed = ShippingSpeed.Standard)
    {
        var toAddress = CreateValidAddress("Customer");
        if (email != null)
        {
            toAddress.Email = email;
        }

        return new CreateShipmentRequest
        {
            Provider = provider,
            Speed = speed,
            FromAddress = CreateValidAddress("Warehouse"),
            ToAddress = toAddress,
            Dimensions = new ShipmentDimensions
            {
                Length = 12,
                Width = 10,
                Height = 8,
                Weight = weight
            }
        };
    }

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }
}
