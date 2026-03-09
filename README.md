# Property Sales Tracker

A comprehensive property sales management system designed for enterprise use.  
The application allows tracking property subscriptions, customer reminders, and performance reporting across different roles in an organization.

?? **Note:** This repository contains a sanitized version for portfolio purposes. The full application contains sensitive company data and is hosted in a private enterprise environment. Only the **login/dashboard screenshot** is included.

---

## Overview

The Property Sales Tracker enables structured management of customers, properties, and account officers with automated notifications for property subscriptions. It is designed for a hierarchical role system:

- **Super Admin:** Creates Admins and Account Officers.  
- **Admin:** Adds properties, creates Account Officers, adds customers, generates reports, and sends manual reminders.  
- **Account Officer:** Can view and manage only customers assigned to them.

---

## Key Features

- Role-based authentication using **ASP.NET Identity**  
- Property and customer management  
- Automated reminders via **SMS and Email** using a Worker Service  
- Customer subscription plans: monthly, quarterly, bi-annual, yearly, or outright  
- Report generation for subscriptions and reminders  
- Bulk import of properties, account officers, and customers via Excel  
- Secure authentication and authorization

---

## Technology Stack

- **Backend:** .NET MVC, Razor Pages  
- **ORM:** Entity Framework Core  
- **Database:** SQL Server  
- **Notifications:** SMS & Email  
- **Background Tasks:** Worker Service for scheduled reminders  
- **Authentication:** ASP.NET Identity

---

## Screenshots

Only the login page is included for security reasons.  
All other screens contain sensitive company data.

![Login Page](docs/login.png)

---

## Deployment

The full application is deployed in a private enterprise environment.  
This public repository is a sanitized version for demonstration and portfolio purposes.

---

## Author

**DevA100** – .NET Developer | Full-stack Portfolio