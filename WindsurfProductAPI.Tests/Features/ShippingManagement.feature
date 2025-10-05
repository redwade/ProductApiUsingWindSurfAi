Feature: Shipping Management
    As an e-commerce manager
    I want to manage shipments through multiple carriers
    So that I can deliver products to customers efficiently

Background:
    Given the API is running
    And the database is empty

Scenario: Get shipping rates from all providers
    When I request shipping rates with the following details:
        | Field          | Value          |
        | FromCity       | San Francisco  |
        | FromState      | CA             |
        | ToCity         | New York       |
        | ToState        | NY             |
        | Weight         | 5              |
    Then I should receive multiple shipping rates
    And the rates should include FedEx options
    And the rates should include UPS options
    And the rates should be sorted by cost

Scenario: Create a shipment with FedEx
    When I create a shipment with the following details:
        | Field          | Value          |
        | Provider       | FedEx          |
        | Speed          | Express        |
        | FromCity       | Los Angeles    |
        | FromState      | CA             |
        | ToCity         | Seattle        |
        | ToState        | WA             |
        | Weight         | 3              |
    Then the shipment should be created successfully
    And the tracking number should start with "FX"
    And the shipment status should be "LabelGenerated"
    And the label URL should be provided

Scenario: Track a shipment by tracking number
    Given I have created a shipment with FedEx
    When I track the shipment using the tracking number
    Then I should see the shipment details
    And the shipment should have tracking updates

Scenario: Get tracking updates for a shipment
    Given I have created a shipment with UPS
    When I request tracking updates
    Then I should see at least 1 tracking update
    And each update should have a timestamp
    And each update should have a location

Scenario: Cancel a pending shipment
    Given I have created a shipment with USPS
    When I cancel the shipment
    Then the shipment should be cancelled successfully
    And the shipment status should be "Cancelled"

Scenario: View shipment history for a customer
    Given the following shipments have been created:
        | Provider | ToEmail              | Weight |
        | FedEx    | user1@example.com    | 5      |
        | UPS      | user1@example.com    | 3      |
        | USPS     | user2@example.com    | 2      |
    When I request shipment history for "user1@example.com"
    Then I should see 2 shipments
    And all shipments should be for "user1@example.com"

Scenario: View all shipment history
    Given the following shipments have been created:
        | Provider | ToEmail              | Weight |
        | FedEx    | user1@example.com    | 5      |
        | UPS      | user2@example.com    | 3      |
    When I request all shipment history
    Then I should see at least 2 shipments

Scenario Outline: Calculate shipping costs for different providers
    When I calculate shipping cost for <Provider> with <Speed> and <Weight> pounds
    Then the calculated cost should be <ExpectedCost>

    Examples:
        | Provider | Speed     | Weight | ExpectedCost |
        | FedEx    | Standard  | 5      | 20.00        |
        | UPS      | Express   | 10     | 45.50        |
        | USPS     | Overnight | 3      | 27.50        |

Scenario Outline: Create shipments with different carriers
    When I create a shipment with <Provider> and <Speed> shipping
    Then the shipment should be created successfully
    And the tracking number should start with "<Prefix>"

    Examples:
        | Provider | Speed     | Prefix |
        | FedEx    | Standard  | FX     |
        | UPS      | Express   | 1Z     |
        | USPS     | TwoDay    | 94     |
        | DHL      | Overnight | DH     |

Scenario: Complete shipping workflow
    Given I want to ship a package from warehouse to customer
    When I request shipping rates
    And I select FedEx Express shipping
    And I create the shipment
    And I track the shipment
    Then the complete workflow should succeed
    And I should have a valid tracking number

Scenario: Attempt to cancel a delivered shipment
    Given I have a shipment that is marked as delivered
    When I attempt to cancel the shipment
    Then the cancellation should fail
    And I should receive an error message about delivered shipments

Scenario: Get shipping rates with invalid dimensions
    When I request shipping rates with zero weight
    Then the request should fail
    And I should receive a validation error
