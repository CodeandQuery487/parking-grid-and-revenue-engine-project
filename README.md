# Parking Grid and Revenue Engine

This is a Parking Management System I built for my second semester project. It is made using C# WinForms and SQL Server.

## About the Project

In this system, users can create an account, log in, park their vehicle by selecting an available slot, and the fee is calculated automatically when they exit. There is a separate admin dashboard where the admin can see total revenue, registered users, and active vehicles.

The database is properly normalized up to 3NF, using lookup tables for vehicle types, slot categories, payment methods, and user roles, instead of storing repeated text values directly in the main tables.

## Features

- User registration and login
- Admin login and dashboard
- 12 parking slots across multiple floors, including Regular, Premium, VIP, and Handicap categories
- Different hourly rates for Car, Bike, Truck, and SUV
- Discount if parked for more than 8 hours
- Park and exit vehicle system
- Admin can generate a report which gets saved on the desktop
- All data is stored in a SQL Server database

## Tech Stack
Language: C#
UI: Windows Forms
Database: SQL Server
Language : TSQL
Data Access: ADO.NET

## OOP Concepts Used
Abstraction - Vehicle abstract class
Inheritance - Car, Bike, and Truck inherit from the Vehicle class
Polymorphism - each vehicle type has its own CalculateFee method
Encapsulation - private fields with public properties
Singleton Pattern - ParkingManager class
Interface - IReportable, implemented by the Admin class

## Database Design
The database file included in this repository (2 semester project.sql) contains the full database script. It includes:

Lookup tables: VehicleTypes, SlotCategories, PaymentMethods, UserRoles
Main tables: Users, ParkingSlots, ParkingRecords, Transactions, Admin, AuditLog
Functions: calculate parking fee, get user total spending, get active vehicles, get daily revenue
Stored procedures: register user, login user, admin login, park vehicle, exit vehicle, get dashboard stats, get all users, generate report, and more
Triggers: auto calculate parked hours, prevent negative wallet balance, audit log for user changes
Views: active vehicles, daily revenue, user summary, slot utilization, monthly revenue, complete parking records, floor wise slots
Indexes on frequently searched columns for better performance

## How to Run

1. Clone this repository
2. Open the project in Visual Studio
3. Open SQL Server Management Studio and run the "2 semester project.sql" file to create the database, tables, functions, procedures, triggers, and views
4. Update the connection string in DatabaseHelper.cs to point to your local SQL Server
5. Build and run the project
6. Login as admin using:
   Username: admin
   Password: admin123

## Project Structure

SimpleParkingSystem folder contains all the C# classes (Vehicle, User, ParkingSlot, ParkingManager, LoginForm, RegisterForm, UserDashboard, AdminDashboard, Program)
DatabaseHelper.cs handles database connection
2 semester project.sql contains the complete database script

## Author

This project was built as a semester assignment. Feel free to check the code and suggest improvements.
