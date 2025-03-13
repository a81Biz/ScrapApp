# Product Scraping Application

## Description

This application will scrape the products automatically from specified websites with the help of Azure Functions. It has robust error handling that can auto-retry when there is failure of the operation and gives detailed breakdown of activity like total products scraped, failures, successful re-tries, and the overall HTTP requests made.

## Features

- **Automated Scraping:** Scrapes product information from web pages automatically.
- **Error Handling:** Tries failed attempts once before recording them as failures.
- **Request Counter:** Monitors and records the HTTP request number to assist with budgeting.
- **Execution Summary:** Provides a summary report at the end of each execution, listing successes and failures.

## Technologies Used

- **C#**
- **Azure Functions**
- **HtmlAgilityPack for HTML parsing.**
- **System.Net.Http for handling HTTP requests.**

## Setup and Use

- **Initial Setup:** Clone this repo onto your local machine or server.
- **Setup Azure Functions:** Ensure your Azure setting is configured to run Functions.
- **Deploy:** Deploy the function into Azure and configure triggers as needed (a timer, e.g., or HTTP event).
- **Monitoring:** Track performance and associated costs through Azure Monitor and Azure Application Insights.

License
Licensed under the MIT License - see LICENSE.md for more information.