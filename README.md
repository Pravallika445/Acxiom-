# AcxiomCRM - Enterprise Customer Relationship Management

AcxiomCRM is a full-featured CRM web application built with **ASP.NET Core 8 MVC**, **Entity Framework Core**, and **ASP.NET Core Identity**.

---

## 🚀 Features

- **Role-Based Access Control (RBAC)**:
  - `Admin`: User management, audit logs, system configuration, full data access.
  - `Manager`: Team lead management, approvals, assignment, and performance dashboards.
  - `Sales`: Opportunity pipelines, customer records, follow-up scheduling.
- **Customer & Lead Management**: Contact tracking, customer code generation, and statuses.
- **Opportunity Pipeline**: Deal tracking through stages (Prospecting, Qualification, Proposal, Won/Lost).
- **Follow-up Reminders**: Activity scheduling with status tracking and history.
- **Security**: Password policies, account lockout protection, HTTP-only secure authentication cookies.

---

## 🔑 Default Seeded Credentials

Upon application startup, the system automatically runs database migrations and seeds the following default accounts:

| Role | Email | Default Password |
|---|---|---|
| **Admin** | `admin@acxiomcrm.com` | `Admin@123` |
| **Manager** | `manager@acxiomcrm.com` | `Manager@123` |
| **Sales** | `sales@acxiomcrm.com` | `Sales@123` |

> ⚠️ *Please change these passwords immediately upon production deployment.*

---

## ☁️ Deploying to Render

This repository is pre-configured with a **Dockerfile** and **render.yaml** for automated deployment on Render.

### Step 1: Connect GitHub Repository on Render
1. Log in to your [Render Dashboard](https://dashboard.render.com/).
2. Click **New +** and select **Web Service** (or **Blueprint**).
3. Connect your repository: `https://github.com/Pravallika445/Acxiom-`.

### Step 2: Configure Web Service
If setting up as a **Web Service**:
- **Name**: `acxiom-crm`
- **Region**: Choose closest to your audience (e.g., Oregon or Frankfurt).
- **Environment / Runtime**: `Docker`
- **Branch**: `main`
- **Plan**: `Free`

### Step 3: Configure Environment Variables
In the Render Web Service settings, add the following Environment Variables under the **Environment** tab:

1. `ASPNETCORE_ENVIRONMENT` = `Production`
2. `ConnectionStrings__DefaultConnection` = `<Your-SQL-Server-Connection-String>`
   *(e.g., Azure SQL, Aiven SQL Server, or AWS RDS MS SQL connection string)*

> The application will automatically execute database migrations and seed default administrative users upon the initial container launch.

---

## 💻 Local Development

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- SQL Server (LocalDB or Express)

### Running Locally
```bash
# Clone the repository
git clone https://github.com/Pravallika445/Acxiom-.git
cd Acxiom-

# Restore dependencies & run
dotnet restore
dotnet run --project AcxiomCRM
```
Navigate to `https://localhost:5001` or `http://localhost:5000` in your browser.
