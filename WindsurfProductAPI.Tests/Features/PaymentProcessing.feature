Feature: Payment Processing with Stripe
    As a customer
    I want to purchase products using Stripe
    So that I can securely complete my transactions

Background:
    Given the API is running
    And the database is empty
    And the following products exist:
        | Name              | Price   | Category    |
        | Wireless Mouse    | 29.99   | Electronics |
        | Laptop Stand      | 49.99   | Accessories |
        | USB Cable         | 9.99    | Electronics |

Scenario: Create a payment intent for a single product
    When I create a payment for product 1 with quantity 1 and email "customer@example.com"
    Then the payment should be created successfully
    And the payment intent ID should not be empty
    And the payment amount should be 29.99
    And the payment status should be valid

Scenario: Create a payment intent for multiple quantities
    When I create a payment for product 2 with quantity 3 and email "customer@example.com"
    Then the payment should be created successfully
    And the payment amount should be 149.97

Scenario: Calculate total price before payment
    When I calculate the price for product 1 with quantity 5
    Then the calculated total should be 149.95

Scenario: Confirm a successful payment
    Given I have created a payment for product 1 with quantity 1
    When I confirm the payment
    Then the payment should be confirmed
    And the payment status should be "succeeded"

Scenario: Cancel a pending payment
    Given I have created a payment for product 2 with quantity 1
    When I cancel the payment
    Then the payment should be canceled
    And the payment status should be "canceled"

Scenario: Retrieve payment status
    Given I have created a payment for product 1 with quantity 2
    When I retrieve the payment status
    Then I should see the payment details
    And the payment amount should be 59.98

Scenario: View payment history for a customer
    Given the following payments have been made:
        | ProductId | Quantity | CustomerEmail        |
        | 1         | 1        | user1@example.com    |
        | 2         | 2        | user1@example.com    |
        | 3         | 1        | user2@example.com    |
    When I request payment history for "user1@example.com"
    Then I should see 2 payments
    And all payments should be for "user1@example.com"

Scenario: View all payment history
    Given the following payments have been made:
        | ProductId | Quantity | CustomerEmail        |
        | 1         | 1        | user1@example.com    |
        | 2         | 1        | user2@example.com    |
    When I request all payment history
    Then I should see at least 2 payments

Scenario: Attempt to create payment for non-existent product
    When I create a payment for product 999 with quantity 1 and email "test@example.com"
    Then the payment creation should fail
    And I should receive an error message

Scenario Outline: Create payments with different quantities
    When I create a payment for product <ProductId> with quantity <Quantity> and email "test@example.com"
    Then the payment should be created successfully
    And the payment amount should be <ExpectedAmount>

    Examples:
        | ProductId | Quantity | ExpectedAmount |
        | 1         | 1        | 29.99          |
        | 1         | 2        | 59.98          |
        | 2         | 1        | 49.99          |
        | 3         | 5        | 49.95          |

Scenario: Complete payment workflow
    Given I want to purchase product 1 with quantity 2
    When I create the payment with email "complete@example.com"
    And I retrieve the payment status
    And I confirm the payment
    Then the payment workflow should complete successfully
    And the final status should be "succeeded"
