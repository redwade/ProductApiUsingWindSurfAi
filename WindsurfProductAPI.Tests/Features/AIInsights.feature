Feature: AI-Powered Product Insights
    As a product manager
    I want to generate AI insights for products
    So that I can optimize product descriptions and pricing

Background:
    Given the API is running
    And the database is empty

Scenario: Generate marketing description for a product
    Given a product exists with the following details:
        | Field       | Value                    |
        | Name        | Wireless Headphones      |
        | Description | Noise-canceling headset  |
        | Price       | 299.99                   |
        | Category    | Electronics              |
    When I request a marketing description for the product
    Then the marketing description should be generated
    And the description should contain the product name
    And the description should contain the price

Scenario: Generate complete AI insights
    Given a product exists with the following details:
        | Field       | Value              |
        | Name        | Smart Watch        |
        | Description | Fitness tracker    |
        | Price       | 399.99             |
        | Category    | Electronics        |
    When I request AI insights for the product
    Then the AI insights should include marketing description
    And the AI insights should include positioning analysis
    And the AI insights should include pricing analysis
    And the AI insights should include category suggestion

Scenario: Analyze product positioning
    Given a product exists with the following details:
        | Field       | Value           |
        | Name        | Premium Laptop  |
        | Description | High-end device |
        | Price       | 2499.99         |
        | Category    | Electronics     |
    When I request positioning analysis for the product
    Then the positioning should indicate "premium" segment
    And the positioning should include target market information

Scenario Outline: Pricing analysis for different price points
    Given a product exists with price <Price>
    When I request pricing analysis for the product
    Then the pricing analysis should classify it as "<Segment>"

    Examples:
        | Price   | Segment         |
        | 29.99   | budget-friendly |
        | 149.99  | mid-range       |
        | 999.99  | premium         |

Scenario: Generate catalog insights
    Given the following products exist:
        | Name       | Price   | Category    |
        | Product 1  | 99.99   | Electronics |
        | Product 2  | 199.99  | Electronics |
        | Product 3  | 49.99   | Books       |
        | Product 4  | 149.99  | Sports      |
    When I request catalog insights
    Then the insights should show 4 total products
    And the insights should show 3 categories
    And the insights should include average price
    And the insights should include recommendations

Scenario: Suggest category for product
    Given a product exists with the following details:
        | Field       | Value                    |
        | Name        | Fitness Tracker Watch    |
        | Description | Track your workouts      |
        | Price       | 199.99                   |
        | Category    | General                  |
    When I request category suggestion for the product
    Then a category should be suggested
    And the suggested category should be relevant to the product
