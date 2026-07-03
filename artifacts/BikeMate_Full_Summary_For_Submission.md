# BikeMate Full Summary For Submission

Use this file as the source content for the PowerPoint/Canva presentation and the PDF project documentation.

Replace `[Group Name]` and the group member placeholders before submitting.

## Submission Title

**[Group Name]_BikeMate Documentation**

## Group Members

List all members here:

- [Member 1 Full Name]
- [Member 2 Full Name]
- [Member 3 Full Name]
- [Member 4 Full Name]
- [Member 5 Full Name]

## Project Overview

BikeMate is a motorcycle repair and service assistance system designed to connect customers with bike shops, mechanics, and emergency repair support. The project provides an Android-first mobile application, a backend web API, a SQL Server database, and an admin web dashboard for managing users, shops, mechanics, service requests, payments, approvals, and emergency cases.

The system addresses the difficulty of finding reliable motorcycle assistance, especially during breakdowns or when customers need scheduled maintenance. Instead of relying on manual searching or social media posts, BikeMate gives users a centralized platform where they can request repair services, browse shops, track assigned mechanics, communicate through messages, manage payments, and request urgent help.

The platform supports multiple user roles:

- Customer: books services, manages motorcycle details, tracks jobs, pays, reviews, and requests emergency help.
- Mechanic/Rider: receives jobs, accepts or rejects requests, updates job status, shares location, manages earnings, and communicates with customers.
- Shop Admin: manages shop profile, services, products, bookings, mechanics, messages, reports, and shop application details.
- System Admin: monitors the whole platform, approves users/shops/mechanics, manages requests and payments, reviews emergency queues, and controls admin accounts.

## Introduction / Background Of The Project

Motorcycle users often need fast and trustworthy repair assistance, but service discovery can be slow and unreliable. Customers may not know which nearby shop is available, which mechanic can respond, how much a service may cost, or how to track the status of a repair request. Shops and mechanics also need a better way to receive bookings, manage job assignments, organize products and services, and build customer trust through verified profiles.

BikeMate was developed to help solve these problems by creating a digital motorcycle service platform. It combines customer booking, shop management, mechanic dispatching, real-time communication, location support, emergency service requests, payment tracking, and administrative verification in one system.

The project uses a .NET-based architecture. The mobile applications are built with .NET MAUI, the backend uses ASP.NET Core Web API, the database uses SQL Server with Entity Framework Core migrations, and the system admin web dashboard uses Blazor. The system includes authentication, role-based access, database-backed records, API endpoints, SignalR hubs for real-time features, and placeholder integrations for Google login, PayMongo checkout, email/OTP, Firebase notifications, and file storage.

## General Objective

To develop BikeMate, a motorcycle service assistance platform that helps customers book motorcycle repair services, connect with mechanics and shops, request emergency help, communicate in real time, and allow administrators to monitor and manage platform activity.

## Specific Objectives

1. Provide a mobile app where customers can register, log in, manage their profile, add motorcycle details, browse shops and services, and create service requests.
2. Allow customers to schedule repairs, upload service details, choose service providers, track service status, view payment records, and submit reviews.
3. Provide an emergency assistance flow where customers can request urgent help, share location, communicate with responders, and track active emergency requests.
4. Provide a mechanic/rider interface where mechanics can view assigned and incoming jobs, accept or reject jobs, update repair progress, manage profile details, and view earnings and ratings.
5. Provide a shop admin app where shop owners can register, submit business information, manage shop profile, products, services, bookings, mechanics, messages, notifications, reports, and settings.
6. Provide a system admin web dashboard for user management, shop verification, mechanic verification, service request monitoring, payment monitoring, emergency queue review, and admin account management.
7. Store data in a structured SQL Server database with entities for users, roles, shops, mechanics, clients, motorcycles, services, products, bookings, messages, payments, reviews, notifications, live locations, and audit logs.
8. Implement secure authentication using JWT, password hashing, OTP support, and role-based authorization.
9. Support real-time platform behavior through SignalR hubs for booking, chat, emergency, location, and notification features.
10. Prepare the system for future integration with payment, email, Google OAuth, Firebase notification, and file storage providers.

## Scope Of The Project

BikeMate covers the main workflows needed for a motorcycle service platform:

- User account registration, login, OTP verification, Google login structure, password reset, logout, and role-based routing.
- Customer profile management, address management, motorcycle records, service booking, scheduling, service request tracking, payments, receipt/invoice screens, chat, notifications, profile, and emergency assistance.
- Mechanic dashboard, job list, active job details, map/location support, emergency requests, chat, profile editing, earnings, ratings, and service history.
- Shop admin registration, shop application submission, valid ID/business permit upload workflow, OTP verification, shop profile, products, services, bookings, mechanics, messages, sales, reports, notifications, settings, and help support.
- System admin dashboard, users list, shop records, approvals, mechanics directory, service requests, emergency requests, payments, admin account management, and detailed review pages.
- Backend API endpoints for authentication, customers, mechanics, shops, shop onboarding, services, products, service requests, payments, conversations/messages, locations, emergency, maps/geography, notifications, devices, files, reports, and admin functions.
- SQL Server database schema and EF Core migrations for all main system records.

The current project is a local/development-ready system. It includes real project structure, database schema, API implementation, mobile screens, web admin screens, test accounts, and local build outputs. Some external provider features are prepared as placeholders until real production keys are configured.

## Project Limitations

- Real PayMongo payment processing requires valid PayMongo public, secret, and webhook keys.
- Real Google OAuth requires a valid Google OAuth client ID.
- Real email/OTP delivery requires SMTP or SendGrid credentials.
- Real push notifications require Firebase credentials.
- File storage can be configured for local or cloud storage, but production storage keys must be supplied separately.
- The project is primarily configured for local development, classroom demo, and Android-first testing.

## Technology Stack

- Mobile app: .NET MAUI, C#, XAML, Android target.
- Shop admin mobile app: .NET MAUI, C#, XAML.
- Backend API: ASP.NET Core Web API, C#.
- Web admin dashboard: Blazor, Razor components, Bootstrap styling.
- Shared project: BikeMate.Core for DTOs, constants, and entities.
- Database layer: Entity Framework Core, SQL Server.
- Database: SQL Server / SQL Server Express.
- Real-time features: SignalR hubs.
- Authentication: JWT bearer tokens, password hashing, role claims.
- Testing: xUnit test project for services, middleware, helpers, and constants.
- Optional integrations: Google OAuth, PayMongo, SMTP/SendGrid, Firebase, file storage.

## System Architecture Summary

BikeMate is divided into several projects:

- `BikeMate.Mobile`: Android-first customer, mechanic, and system admin mobile app screens.
- `BIKEMATES_ADMIN`: .NET MAUI shop admin mobile app for shop owner workflows.
- `BikeMate.Api`: ASP.NET Core backend API that handles business logic, authentication, service requests, messaging, payments, location, emergency, and admin endpoints.
- `BikeMate.Core`: Shared DTOs, constants, and entity definitions used by the API and mobile clients.
- `BikeMate.Infrastructure`: Entity Framework Core database context, migrations, and SQL scripts.
- `BikeMate.WebAdmin`: Blazor system admin dashboard.
- `BikeMate.Tests`: Unit tests for backend services, constants, middleware, and helpers.

## Database Summary

The database stores the main BikeMate platform records. Important entities include:

- Users, Roles, UserRoles, AuthProviders, OTP records, password reset tokens, and device tokens.
- Clients, client addresses, and motorcycles.
- Mechanics, mechanic availability, assigned shop mechanics, ratings, completed job counts, and live locations.
- Shops, shop operating hours, shop services, service categories, products, product images, and service images.
- Service requests, request status, request status history, request media, and live tracking.
- Conversations, conversation participants, and messages.
- Payments, payment statuses, payment methods, and payment events.
- Reviews, notifications, and audit logs.

The project includes EF Core migrations and SQL scripts:

- `BikeMate.Infrastructure\Migrations`
- `BikeMate.Infrastructure\Scripts\BikeMate_InitialSchema.sql`
- `BikeMate.Infrastructure\Scripts\BikeMate_RunThis_DatabaseSetup.sql`
- `BikeMate.Infrastructure\Scripts\sql-server-triggers.sql`

## System Demonstration Script

Use this flow when presenting the system.

### 1. Onboarding And Login

Show the BikeMate onboarding screens, then proceed to login. Explain that users can log in based on their role. The app stores JWT tokens securely and routes users to the correct shell based on role.

Suggested screenshots:

- `bikemate-login-check.png`
- `bikemate-login-check2.png`
- `bikemate-after-onboarding.png`
- `bikemate-after-onboarding2.png`

### 2. Customer Home

Show the customer home page. Explain that customers can view available service options, access their schedule, messages, payments, help, profile, and booking functions.

Suggested screenshots:

- `bikemate-customer-home.png`
- `bikemate-customer-home-fresh.png`

### 3. Booking A Service

Show the booking flow. Explain that the customer can provide repair details, select service type, choose schedule, upload details/images, confirm the booking, search/select a shop, and track the service request.

Suggested screenshots:

- `bikemate-booking.png`
- `bikemate-schedule.png`
- `bikemate-schedule-final.png`

### 4. Messages And Chat

Show the messages/chat screen. Explain that customers and mechanics/shop representatives can communicate regarding service details and updates.

Suggested screenshots:

- `bikemate-messages.png`
- `bikemate-chat.png`

### 5. Payments, Receipt, And Invoice

Show payment screens. Explain that customers can view payment options, payment details, receipts, and invoices. The backend has PayMongo checkout support prepared, while real transactions require production keys.

Suggested screenshots:

- `bikemate-payments.png`
- `bikemate-payment-options.png`
- `bikemate-payment-details.png`
- `bikemate-receipt.png`
- `bikemate-invoice.png`

### 6. Profile And Help

Show the profile/help pages. Explain that users can manage account information and access support information.

Suggested screenshots:

- `bikemate-profile.png`
- `bikemate-help-fixed.png`

### 7. Emergency Assistance

Show or describe the emergency flow. Explain that a customer can create an emergency repair request, share location, find nearby responders, start a call session, and track the active emergency. The backend includes emergency endpoints and SignalR support.

Main emergency features:

- Emergency SOS page.
- Emergency location picker.
- Calling emergency page.
- Emergency live call page.
- Active emergency tracking page.
- Admin emergency queue in the web dashboard.
- Mechanic emergency request screen.

### 8. Mechanic Workflow

Show the mechanic dashboard and job pages. Explain that mechanics can view jobs, accept/reject requests, update job status, use map/location features, respond to emergencies, chat, view history, earnings, and ratings.

Main mechanic screens:

- Dashboard.
- Jobs.
- Job details.
- Map.
- Emergency requests.
- Messages/chat.
- Profile/edit profile.
- Earnings.
- Ratings.
- History.

### 9. Shop Admin Workflow

Show the shop admin app. Explain that shop owners can create an account, submit shop and owner verification documents, verify email by OTP, wait for system admin approval, and manage their shop after approval.

Main shop admin screens:

- Login and forgot password.
- Account creation steps.
- Shop application review page.
- Dashboard/home.
- Shop profile.
- Products.
- Services.
- Bookings.
- Mechanics.
- Messages.
- Sales.
- Reports.
- Notifications.
- Settings.
- Help/support.

### 10. System Admin Web Dashboard

Show the web admin dashboard. Explain that system admins manage platform-wide operations, including users, shops, approvals, mechanics, service requests, emergency requests, payments, and admin accounts.

Main web admin pages:

- Dashboard: platform summary.
- Emergency: urgent service queue.
- Users: customer account records.
- Shops: partner shop records.
- Approvals: manual verification for customers, mechanics, and shops.
- Mechanics: technician directory.
- Requests: booked repair requests.
- Payments: transactions.
- Admin Accounts: system access management.

## Backend API Summary

Important backend features:

- Authentication: register, login, Google login structure, OTP verification, password reset, logout, and current user profile.
- Customer APIs: profile, address, motorcycles, dashboard, home status.
- Mechanic APIs: mechanic profile, nearby mechanics, jobs, active jobs, accept/reject job, update job status, completion photo, location update.
- Rider APIs: dashboard, online/offline status, incoming requests, emergency jobs, current jobs, history, earnings, ratings.
- Shop APIs: shop dashboard, profile, application, services, bookings, mechanics, inventory, payments, reviews, analytics.
- Shop onboarding APIs: shop owner application, shop registration, shop existence check, application status.
- Service request APIs: create request, active/my requests, status updates, cancellation, mechanic assignment, media upload, shop selection, timeline, upcoming, history.
- Emergency APIs: create emergency request, get emergency status, conversation, cancel, accept, start/end call, nearby responders.
- Payments APIs: create checkout session, webhook, request payment, payment details, status refresh, payment history.
- Messages APIs: conversations, start conversation, get messages, send messages, mark read.
- Admin APIs: dashboard, users, customers, mechanics, shops, pending approvals, verification decisions, requests, emergency requests, payments, revenue, top services, top mechanics, audit logs, announcements.
- Reports APIs: revenue, top services, top mechanics.
- Utility APIs: health, geography, maps, files, notifications, device tokens.

SignalR hubs:

- `/hubs/booking`
- `/hubs/chat`
- `/hubs/emergency`
- `/hubs/location`
- `/hubs/notification`

## Test And Verification Summary

The project includes a final test report showing that the following passed:

- Database migration.
- Seed users and lookup data.
- API build and run.
- Health endpoint.
- Login for Customer, Mechanic, Shop Admin, and System Admin.
- Role-based authorization.
- Customer, Mechanic, Shop Admin, and System Admin API data.
- Chat REST endpoints.
- Payment checkout placeholder.
- Android project build.
- Emulator launch and onboarding image verification.
- Web/admin and API project structure verification.

Development test accounts:

| Role | Email | Password |
| --- | --- | --- |
| Customer | `customer@bikemate.test` | `Password123!` |
| Mechanic | `mechanic@bikemate.test` | `Password123!` |
| Shop Admin | `shop@bikemate.test` | `Password123!` |
| System Admin | `admin@bikemate.test` | `Password123!` |

## Recommended PowerPoint / Canva Slide Content

### Slide 1: Title Page

**BikeMate: Motorcycle Service Assistance System**

Prepared by: [Group Name]

Members:

- [Member 1]
- [Member 2]
- [Member 3]
- [Member 4]
- [Member 5]

### Slide 2: Introduction / Background

BikeMate is a motorcycle service assistance platform that connects motorcycle users with repair shops, mechanics, and emergency responders. It was created to make motorcycle service booking easier, faster, and more organized by using a mobile app, backend API, database, and admin dashboard.

### Slide 3: Problem Statement

Motorcycle users often experience difficulty finding reliable repair assistance, especially during breakdowns. Shops and mechanics also need an organized way to receive bookings, assign jobs, manage services, and build customer trust. BikeMate solves this by centralizing booking, communication, tracking, payments, verification, and administration.

### Slide 4: Objectives

- Enable customers to request motorcycle services and emergency help.
- Allow mechanics to receive, accept, and update jobs.
- Allow shops to manage services, products, bookings, and mechanics.
- Allow system admins to verify users, shops, mechanics, requests, and payments.
- Store platform records in a structured SQL Server database.
- Support secure login, role-based access, and real-time communication.

### Slide 5: Scope

BikeMate includes customer mobile features, mechanic mobile features, shop admin mobile features, a backend API, a SQL Server database, and a web-based system admin dashboard. It covers registration, login, booking, scheduling, tracking, messaging, payments, reviews, emergency assistance, approvals, and reporting.

### Slide 6: System Architecture

The system is composed of:

- .NET MAUI mobile apps.
- ASP.NET Core Web API.
- SQL Server database with EF Core.
- Blazor system admin web dashboard.
- Shared Core project for DTOs/entities.
- SignalR hubs for real-time updates.

### Slide 7: Main User Roles

- Customer: books services, tracks jobs, pays, chats, reviews, and requests emergency assistance.
- Mechanic/Rider: accepts jobs, updates repair status, shares location, views earnings and ratings.
- Shop Admin: manages shop profile, services, products, bookings, mechanics, and reports.
- System Admin: manages users, approvals, shops, mechanics, requests, emergency cases, payments, and admin accounts.

### Slide 8: Customer Demonstration

Show onboarding, login, home, booking, scheduling, chat, payments, receipt/invoice, profile, and emergency assistance. Explain how a customer creates a repair request and follows it until completion.

### Slide 9: Mechanic And Shop Admin Demonstration

Show mechanic job management and shop admin management features. Explain that mechanics handle repair requests while shop admins manage services, products, mechanics, bookings, and reports.

### Slide 10: System Admin Demonstration

Show the Blazor web dashboard. Explain how admins monitor platform activity, approve users/shops/mechanics, review service requests and emergency requests, check payments, and manage admin accounts.

### Slide 11: Database And API

BikeMate stores users, roles, customers, mechanics, shops, motorcycles, services, products, service requests, messages, payments, reviews, notifications, locations, and audit logs. The API provides endpoints for authentication, bookings, messaging, payments, emergency, reports, and admin management.

### Slide 12: Conclusion

BikeMate provides an integrated system for motorcycle service booking and assistance. It improves customer access to repair services, helps mechanics and shops manage work, and gives administrators tools to verify and monitor platform operations.

## Documentation File Content

For the PDF documentation, use these sections:

1. Title Page
2. Group Members
3. Introduction / Background Of The Project
4. Objectives
5. Scope
6. System Architecture
7. Technology Stack
8. Database Design Summary
9. System Demonstration With UI Descriptions
10. Testing And Verification
11. Limitations
12. Conclusion
13. Appendix: Source Files Included

Recommended PDF filename:

`[GroupName]_Documentation.pdf`

## Source Files To Include In Submission

Include the full project folder if the portal allows ZIP upload. If uploading by category, use this mapping.

### Mobile App Code

- `BikeMate.Mobile`
- `BIKEMATES_ADMIN`

### Web Service / API Code

- `BikeMate.Api`
- `BikeMate.Core`
- `BikeMate.Infrastructure`

### Database Schema And Associated Files

- `BikeMate.Infrastructure\Migrations`
- `BikeMate.Infrastructure\Scripts`
- `tools\Reset-DevData.sql`
- `tools\Prepare-DemoDatabase.ps1`

### Web App Files

- `BikeMate.WebAdmin`

### Tests And Supporting Documentation

- `BikeMate.Tests`
- `README.md`
- `BIKEMATE_SETUP_GUIDE.md`
- `BIKEMATE_API_SETUP_GUIDE.md`
- `API_TESTING_GUIDE.md`
- `FINAL_TEST_REPORT.md`
- `PROJECT_INSPECTION_REPORT.md`
- `BEGINNER_REQUIREMENTS_CHECKLIST.md`
- `RUN_BIKEMATE_STEP_BY_STEP.md`

### Presentation And Demo Assets

- `bikemate-login-check.png`
- `bikemate-login-check2.png`
- `bikemate-after-onboarding.png`
- `bikemate-after-onboarding2.png`
- `bikemate-customer-home.png`
- `bikemate-customer-home-fresh.png`
- `bikemate-booking.png`
- `bikemate-schedule.png`
- `bikemate-schedule-final.png`
- `bikemate-messages.png`
- `bikemate-chat.png`
- `bikemate-payments.png`
- `bikemate-payment-options.png`
- `bikemate-payment-details.png`
- `bikemate-receipt.png`
- `bikemate-invoice.png`
- `bikemate-profile.png`
- `bikemate-help-fixed.png`

### Build Artifacts If Required By Instructor

- `artifacts\phone-demo\apk\BikeMate.Mobile.Debug.apk`
- `artifacts\phone-demo\apk\BIKEMATES_ADMIN.Debug.apk`

## Short Abstract

BikeMate is a motorcycle service assistance system that connects customers, mechanics, shops, and system administrators through a mobile app, backend API, web dashboard, and SQL Server database. The system allows customers to book motorcycle repair services, request emergency assistance, track repairs, chat with service providers, manage payments, and submit reviews. Mechanics can accept jobs and update repair progress, while shop admins can manage services, products, bookings, mechanics, and reports. System admins can monitor the platform, approve users and shops, manage requests, review payments, and handle emergency queues. BikeMate improves the organization, accessibility, and reliability of motorcycle service support.

## Short Conclusion

BikeMate successfully demonstrates a full-stack motorcycle service platform with mobile, backend, database, and web admin components. The project supports the major workflows needed by customers, mechanics, shop owners, and system administrators. Although real third-party keys are still needed for production payment, OAuth, email, notification, and cloud storage integrations, the current system is ready for local demonstration and shows a strong foundation for a real motorcycle service marketplace.
