# Donately 🚀

Donately is a donation and fundraising platform built with ASP.NET Core 9.
It allows users to create and browse fundraising campaigns, support them through LiqPay, complete verification workflows, and track platform activity through a statistics dashboard.

## Project Documentation 📚

The repository now includes a documentation bundle that explains the solution visually:

- `Documents/architecture_donately.png` — high-level application architecture;
- `Documents/er_diagram.png` — database model and relationships;
- `Documents/sequence_create.png` — fundraiser creation flow;
- `Documents/sequence_donation.png` — donation and payment flow;
- `Documents/use_cases/usecase_guest.png` — guest browsing and public access;
- `Documents/use_cases/usecase_user_auth.png` — authenticated user actions;
- `Documents/use_cases/usecase_user_verif.png` — verification-related use cases.

These artifacts complement the README and make the project easier to present as a portfolio piece or coursework submission.

## Core User Flows 👥

### For guests

- browse public fundraisers;
- open fundraiser details;
- review campaign progress and public information.

### For donors

- discover active fundraisers;
- open a fundraiser details page;
- donate through LiqPay;
- leave a donation anonymous if needed;
- track the success of campaigns.

### For fundraiser owners

- sign in and complete the profile;
- submit verification documents;
- create a new fundraiser;
- upload a cover image and supporting files;
- manage profile data and public information.

### For moderation and platform oversight

- review verification requests;
- approve or reject verification steps;
- inspect statistics and donation activity;
- monitor system health through centralized logging.

## Overview ✨

Donately is designed as a portfolio-ready web application that demonstrates:

- authenticated user flows with ASP.NET Identity;
- fundraiser creation and browsing;
- online payments via LiqPay;
- email, phone, and document verification;
- profile management;
- service-based application logic;
- layered architecture with a clear separation of concerns.

The UI is implemented with Razor Views, while persistence and integrations are handled through Entity Framework Core, PostgreSQL, SendGrid, and LiqPay.

## Key Features 🔹

- Create fundraising campaigns with a title, short description, full description, category, goal amount, currency, urgency flag, cover image, and attachments.
- Follow the documented fundraiser creation flow from draft to publication.
- Browse active fundraisers with filtering by category, status, currency, and maximum goal amount.
- View fundraiser details, attachments, and donation progress.
- Support a fundraiser through LiqPay with a payment checkout flow.
- Follow the documented donation sequence from checkout creation to callback confirmation.
- Submit anonymous donations without storing donor details in the database.
- Manage a personal profile with avatar, bio, location, email, phone number, and date of birth.
- Complete verification flows for email, phone, and documents.
- Review platform-wide statistics and fundraising activity.
- View a recent donations feed on the home page.
- Log requests and handle errors through centralized middleware.

## Architecture 🧩

Donately follows a layered / Onion Architecture approach, where the UI depends on application contracts, the application layer depends on abstractions, and infrastructure provides the concrete implementations. The architecture diagram in `Documents/architecture_donately.png` shows the same dependency direction.

```text
				┌──────────────────────────────┐
				│        Presentation          │
				│   MVC Controllers + Views    │
				└──────────────┬───────────────┘
				               │
				┌──────────────▼────────────────┐
				│         Application           │
				│  Interfaces + View Models     │
				└──────────────┬────────────────┘
				               │
				┌──────────────▼────────────────┐
				│        Infrastructure         │
				│EF Core + Services + Middleware│
				└──────────────┬────────────────┘
				               │
				┌──────────────▼─────────────────┐
				│      PostgreSQL / APIs         │
				│   Database + LiqPay + SendGrid │
				└────────────────────────────────┘

		                   Domain: shared business model
```

### What lives in each layer

- `Domain` — core business entities and enums.
- `Application` — interfaces, common option objects, result models, and view models.
- `Infrastructure` — Entity Framework Core persistence, external services, middleware, and background services.
- `Presentation` — MVC controllers, Razor Views, and static web assets.

### Why the structure matters

- `Domain` keeps business rules independent from frameworks;
- `Application` defines use cases, contracts, and presentation models;
- `Infrastructure` provides persistence, payment integration, email delivery, and verification services;
- `Presentation` keeps the web UI thin and focused on request handling.

### Persistence model

`ApplicationDbContext` extends `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and maps the main data model shown in `Documents/er_diagram.png`:

- `Users`
- `Fundraisers`
- `Donations`
- `PaymentTransactions`
- `Attachments`
- `VerificationRequests`
- `AuditLogs`
- `WebhookEvents`

## Technology Stack 🛠️

- ASP.NET Core 9
- Razor Views
- Entity Framework Core 9
- PostgreSQL
- ASP.NET Identity
- Serilog
- SendGrid
- LiqPay API

## Project Structure 📁

```text
src/
├── Application/      # interfaces, options, view models, shared application types
├── Domain/           # business entities and enums
├── Infrastructure/   # data access, services, middleware, hosted services
├── Migrations/       # EF Core database migrations
├── Presentation/     # controllers, views, and static assets
├── Program.cs        # application bootstrap and dependency injection
└── appsettings*.json # configuration
```

## Getting Started 🚦

### Prerequisites

- .NET 9 SDK
- PostgreSQL
- SendGrid account and API key
- LiqPay merchant credentials
- `ngrok` or another public tunnel for local payment callbacks

### Configuration

The application reads configuration from `src/appsettings.json` and user secrets.

Required settings:

| Section | Key | Purpose |
| --- | --- | --- |
| `ConnectionStrings` | `DefaultConnection` | PostgreSQL connection string |
| `SendGrid` | `SendGridKey` | API key for email delivery |
| `SendGrid` | `FromAddress` | Sender email address |
| `SendGrid` | `FromName` | Sender display name |
| `LiqPay` | `PublicKey` | Public merchant key |
| `LiqPay` | `PrivateKey` | Private merchant key |
| `LiqPay` | `PublicBaseUrl` | Public base URL used for callback URLs |
| `LiqPay` | `CheckoutUrl` | LiqPay checkout endpoint |
| `LiqPay` | `Sandbox` | Enables sandbox mode for local testing |

Example user-secrets setup:

```bash
dotnet user-secrets set --project src/src.csproj "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=Donately;Username=postgres;Password=your_password"
dotnet user-secrets set --project src/src.csproj "SendGrid:SendGridKey" "your_sendgrid_api_key"
dotnet user-secrets set --project src/src.csproj "LiqPay:PublicKey" "your_public_key"
dotnet user-secrets set --project src/src.csproj "LiqPay:PrivateKey" "your_private_key"
dotnet user-secrets set --project src/src.csproj "LiqPay:PublicBaseUrl" "https://your-public-tunnel-url.ngrok-free.app"
```

### Database setup

Apply the migrations before running the application:

```bash
dotnet ef database update --project src/src.csproj --startup-project src/src.csproj
```

The migrations also create the required PostgreSQL `pgcrypto` extension if the database user has permission to do so.

### Run locally

The launch settings expose the application on:

- `https://localhost:7214`
- `http://localhost:5258`

Run the application from the repository root:

```bash
dotnet run --project src/src.csproj
```

### Testing LiqPay locally

1. Start the application.
2. Expose the HTTPS endpoint through a tunnel:

```bash
ngrok http https://localhost:7214
```

3. Copy the generated public URL and set it as `LiqPay:PublicBaseUrl`.
4. Open the application through the tunnel URL and test the donation flow.

Because the app uses forwarded headers, it is safe to run behind a reverse proxy or tunnel for callback testing. This matches the callback-oriented donation flow shown in `Documents/sequence_donation.png`.

## Author 👨‍💻

Andrii Kahnovets    
Faculty of Applied Mathematics and Informatics, Ivan Franko National University of Lviv
