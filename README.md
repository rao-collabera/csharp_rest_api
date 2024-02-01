---
page_type: sample
languages:
- csharp
- tsql
products:
- azure
- dotnet-core
- azure-key-vault
- sql-server
- ocelot-api-gateway
- rabbitmq
- graph-api
description: "Creating a minimal API with .NET Core using Sql Server"
urlFragment: "csharp-rest-api"
---
# Create a minimal API with .NET Core

## Features
You can use JSON requests with any capitalization to retrieve JSON responses using SQL Server’s built-in JSON format support, without the need for additional classes or objects.

You can define dynamic actions and handle requests with controllers.

You can see the Json request samples in the Swagger UI, along with the action methods in a dropdown menu.

You can validate your Json requests with Json schemas to ensure data quality and consistency.

You can use Ocelot API Gateway to streamline the routing and authentication of your requests.

You can monitor the health and performance of your microservices, RabbitMQ, and SQL Servers with Health Checks service.

You can communicate and coordinate with other microservices using RabbitMQ, which allows you to publish, subscribe, and receive events.

You can access, analyze, and enhance data from Microsoft 365 with Graph API.

> [!TIP]
> **JSON Schema** serves multiple purposes, one of which is **validating JSON instances**. These examples will help you make the most of your JSON Schemas: https://json-schema.org/learn/miscellaneous-examples

> [!NOTE]  
> The JsonRequests, JsonSchemas, and WebApiActions action names should be excluded from the sprepo.json file. These actions are utilized to retrieve data from the SQL server and populate JSON files during the initial setup.

## Prerequisites
**Microsoft Azure Settings:**
- Web Redirect URIs: https://localhost:44371/swagger/oauth2-redirect.html

- **API / Permissions:**
	- *Microsoft Graph:*
		- Calendars.Read => Delegated
		- Calendars.ReadBasic => Delegated
		- Contacts.Read => Delegated
		- Mail.Read => Delegated
		- Mail.ReadBasic => Delegated
		- Mail.ReadWrite => Delegated
		- Mail.Send => Delegated
		- MailboxSettings.Read => Delegated
		- Tasks.Read => Delegated
		- Tasks.ReadWrite => Delegated
		- People.Read => Delegated
		- Profile => Delegated
		- User.Read => Delegated
		- User.ReadAll => Delegated
		- Directory Roles => RoleManagement.Read.Directory => Delegated (If you want to read directory)

## Contributing
This project welcomes contributions and suggestions.

## Credits
This GitHub repository demonstrates the utilization of SQL Server’s native JSON support: https://github.com/Azure-Samples/azure-sql-db-dotnet-rest-api

Thanks to my boss Dhar Patadia (dhar@collabera.com), who is very supportive and innovative, for initiating this project.