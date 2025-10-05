Feature: Product Management
    As a product manager
    I want to manage products in the catalog
    So that I can maintain an up-to-date product inventory

Background:
    Given the API is running
    And the database is empty

Scenario: Create a new product
    When I create a product with the following details:
        | Field       | Value                    |
        | Name        | Wireless Headphones      |
        | Description | Premium noise-canceling  |
        | Price       | 299.99                   |
        | Category    | Electronics              |
    Then the product should be created successfully
    And the product should have an ID
    And the product name should be "Wireless Headphones"
    And the product price should be 299.99

Scenario: Retrieve all products
    Given the following products exist:
        | Name            | Description      | Price  | Category    |
        | Laptop Pro      | High-end laptop  | 1299.99| Electronics |
        | Yoga Mat        | Premium mat      | 49.99  | Sports      |
        | Coffee Maker    | Smart appliance  | 199.99 | Home        |
    When I request all products
    Then I should receive 3 products
    And the products should include "Laptop Pro"
    And the products should include "Yoga Mat"
    And the products should include "Coffee Maker"

Scenario: Update an existing product
    Given a product exists with the following details:
        | Field       | Value           |
        | Name        | Old Product     |
        | Description | Old description |
        | Price       | 99.99           |
        | Category    | General         |
    When I update the product with:
        | Field       | Value           |
        | Name        | New Product     |
        | Description | New description |
        | Price       | 149.99          |
        | Category    | Premium         |
    Then the product should be updated successfully
    And the product name should be "New Product"
    And the product price should be 149.99

Scenario: Delete a product
    Given a product exists with name "Product to Delete"
    When I delete the product
    Then the product should be deleted successfully
    And the product should not be found when retrieved

Scenario: Retrieve non-existing product
    When I request a product with ID 99999
    Then I should receive a not found response

Scenario Outline: Create products with different prices
    When I create a product with name "<Name>" and price <Price>
    Then the product should be created successfully
    And the product price should be <Price>

    Examples:
        | Name              | Price   |
        | Budget Item       | 9.99    |
        | Mid-range Item    | 99.99   |
        | Premium Item      | 999.99  |
        | Luxury Item       | 9999.99 |
