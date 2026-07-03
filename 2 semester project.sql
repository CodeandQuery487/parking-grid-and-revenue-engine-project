

IF DB_ID('MyParkingDB') IS NOT NULL
BEGIN
    ALTER DATABASE MyParkingDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE MyParkingDB;
END
GO

CREATE DATABASE MyParkingDB;
GO

USE MyParkingDB;
GO

GO

-- 1. lookup tables  (For 2NF - removes partial dependencies)

-- Vehicle Types Lookup
CREATE TABLE VehicleTypes (
    VehicleTypeID INT PRIMARY KEY IDENTITY(1,1),
    TypeName VARCHAR(20) NOT NULL UNIQUE,
    BaseRate DECIMAL(10,2) NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Slot Categories Lookup
CREATE TABLE SlotCategories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName VARCHAR(20) NOT NULL UNIQUE,
    PremiumPercent DECIMAL(5,2) NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Payment Methods Lookup
CREATE TABLE PaymentMethods (
    PaymentID INT PRIMARY KEY IDENTITY(1,1),
    MethodName VARCHAR(30) NOT NULL UNIQUE,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- User Roles Lookup (For 3NF - Removes Transitive Dependency)
CREATE TABLE UserRoles (
    RoleID INT PRIMARY KEY IDENTITY(1,1),
    RoleName VARCHAR(30) NOT NULL UNIQUE,
    Description VARCHAR(200) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Insert User Roles
INSERT INTO UserRoles (RoleName, Description) VALUES
('Customer', 'Regular parking customer'),
('Staff', 'Parking staff member'),
('Manager', 'Parking manager'),
('Admin', 'System administrator');
GO

-- 2. MAIN ENTITY TABLES (3NF - No Transitive Dependencies)

-- Users Table (3NF: All columns depend only on UserID)
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    RoleID INT NOT NULL DEFAULT 1 
        CONSTRAINT FK_Users_RoleID FOREIGN KEY REFERENCES UserRoles(RoleID),
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL 
        CONSTRAINT UQ_Users_Email UNIQUE,
    Phone VARCHAR(15) NOT NULL,
    CNIC VARCHAR(20) NOT NULL 
        CONSTRAINT UQ_Users_CNIC UNIQUE,
    Username VARCHAR(50) NOT NULL UNIQUE,
    UserPassword VARCHAR(100) NOT NULL,
    WalletBalance DECIMAL(10,2) NOT NULL DEFAULT 0 
        CONSTRAINT CK_Users_WalletBalance CHECK (WalletBalance >= 0),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastLoginDate DATETIME NULL,
    CONSTRAINT CK_Users_Email CHECK (Email LIKE '%_@_%_%'),
    CONSTRAINT CK_Users_Phone CHECK (LEN(Phone) >= 10)
);
GO

-- Parking Slots Table (3NF: All columns depend on SlotID)
CREATE TABLE ParkingSlots (
    SlotID INT PRIMARY KEY IDENTITY(1,1),
    SlotNumber VARCHAR(5) NOT NULL UNIQUE 
        CONSTRAINT CK_SlotNumber CHECK (SlotNumber LIKE 'S[0-9][0-9]'),
    FloorNo INT NOT NULL,
    CategoryID INT NOT NULL 
        CONSTRAINT FK_ParkingSlots_CategoryID FOREIGN KEY REFERENCES SlotCategories(CategoryID),
    IsAvailable BIT NOT NULL DEFAULT 1,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Parking Records Table (1NF: this table is in 1nf ,no repeating values)
CREATE TABLE ParkingRecords (
    RecordID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NULL CONSTRAINT FK_ParkingRecords_UserID FOREIGN KEY REFERENCES Users(UserID),
    SlotID INT NOT NULL CONSTRAINT FK_ParkingRecords_SlotID FOREIGN KEY REFERENCES ParkingSlots(SlotID),
    PlateNumber VARCHAR(20) NOT NULL,
    VehicleTypeID INT NOT NULL CONSTRAINT FK_ParkingRecords_VehicleTypeID FOREIGN KEY REFERENCES VehicleTypes(VehicleTypeID),
    OwnerName VARCHAR(100) NOT NULL,
    OwnerCNIC VARCHAR(20) NOT NULL,
    EntryTime DATETIME NOT NULL DEFAULT GETDATE(),
    ExitTime DATETIME NULL,
    HoursParked INT NULL,
    TotalAmount DECIMAL(10,2) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- Transactions Table (3NF: No transitive dependencies)
CREATE TABLE Transactions (
    TransID INT PRIMARY KEY IDENTITY(1,1),
    RecordID INT NOT NULL 
        CONSTRAINT FK_Transactions_RecordID FOREIGN KEY REFERENCES ParkingRecords(RecordID),
    UserID INT NULL 
        CONSTRAINT FK_Transactions_UserID FOREIGN KEY REFERENCES Users(UserID),
    PaymentID INT NOT NULL 
        CONSTRAINT FK_Transactions_PaymentID FOREIGN KEY REFERENCES PaymentMethods(PaymentID),
    Amount DECIMAL(10,2) NOT NULL 
        CONSTRAINT CK_Transactions_Amount CHECK (Amount > 0),
    TransTime DATETIME NOT NULL DEFAULT GETDATE(),
    IsRefunded BIT NOT NULL DEFAULT 0,
    RefundReason VARCHAR(200) NULL
);
GO

-- Admin Table (Separate from Users for security - 3NF)
CREATE TABLE Admin (
    AdminID INT PRIMARY KEY IDENTITY(1,1),
    Username VARCHAR(50) NOT NULL UNIQUE,
    AdminPassword VARCHAR(100) NOT NULL,
    FullName VARCHAR(100) NOT NULL,
    Role VARCHAR(30) NOT NULL DEFAULT 'Staff',
    LastLogin DATETIME NULL,
    CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- Audit Log Table (For tracking changes)
CREATE TABLE AuditLog (
    AuditID INT PRIMARY KEY IDENTITY(1,1),
    TableName VARCHAR(50) NOT NULL,
    ActionType VARCHAR(20) NOT NULL,
    RecordID INT NOT NULL,
    OldValue VARCHAR(MAX) NULL,
    NewValue VARCHAR(MAX) NULL,
    ChangedBy VARCHAR(50) DEFAULT SUSER_NAME(),
    ChangeTime DATETIME DEFAULT GETDATE()
);
GO

-- 3. INDEXES (For Performance)

-- Clustered index is automatically created on primary key

-- Non clustered indexes
CREATE INDEX IX_Users_Username ON Users(Username);
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_RoleID ON Users(RoleID);
CREATE INDEX IX_ParkingRecords_IsActive ON ParkingRecords(IsActive);
CREATE INDEX IX_ParkingRecords_EntryTime ON ParkingRecords(EntryTime DESC);
CREATE INDEX IX_Transactions_TransTime ON Transactions(TransTime DESC);
CREATE INDEX IX_ParkingSlots_IsAvailable ON ParkingSlots(IsAvailable);

-- Composite Index (For better query performance)
CREATE INDEX IX_ParkingRecords_User_Active ON ParkingRecords(UserID, IsActive);

-- Filtered Index (Only active users - 3NF benefit)
CREATE INDEX IX_Users_Active ON Users(IsActive) WHERE IsActive = 1;

-- Covering Index
CREATE INDEX IX_ParkingSlots_Available_Cover ON ParkingSlots(IsAvailable) INCLUDE (SlotNumber, FloorNo, CategoryID);
GO

-- 4. inserting initial data in tbles  (Normalized)

INSERT INTO VehicleTypes (TypeName, BaseRate) VALUES
('Car', 50), ('Bike', 20), ('Truck', 100), ('SUV', 70);
GO

INSERT INTO SlotCategories (CategoryName, PremiumPercent) VALUES
('Regular', 0), ('Premium', 20), ('VIP', 50), ('Handicap', 0);
GO

INSERT INTO PaymentMethods (MethodName) VALUES
('Cash'), ('Credit Card'), ('Debit Card'), ('Mobile Wallet');
GO

INSERT INTO ParkingSlots (SlotNumber, FloorNo, CategoryID) VALUES
('S01', 1, 1), ('S02', 1, 1), ('S03', 1, 2), ('S04', 1, 1),
('S05', 1, 4), ('S06', 2, 1), ('S07', 2, 2), ('S08', 2, 1),
('S09', 2, 1), ('S10', 2, 1), ('S11', 3, 2), ('S12', 3, 3);
GO

INSERT INTO Admin (Username, AdminPassword, FullName, Role) VALUES
('admin', 'admin123', 'System Administrator', 'SuperAdmin'),
('manager1', 'manager123', 'Nimra manager', 'Manager');
GO

INSERT INTO Users (RoleID, FullName, Email, Phone, CNIC, Username, UserPassword, WalletBalance) VALUES
(1, 'Nimra', 'nimra@email.com', '03001234567', '12345-6789012-3', 'nimra123', 'pass123', 500);
GO

-- 5. FUNCTIONS

-- Function 1 calculate parking fee  (Scalar function )
CREATE FUNCTION fn_CalculateFee(
    @VehicleTypeID INT,
    @Hours INT,
    @CategoryID INT
)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @BaseRate DECIMAL(10,2);
    DECLARE @PremiumPercent DECIMAL(5,2);
    DECLARE @Fee DECIMAL(10,2);
    
    SELECT @BaseRate = BaseRate FROM VehicleTypes WHERE VehicleTypeID = @VehicleTypeID;
    SELECT @PremiumPercent = PremiumPercent FROM SlotCategories WHERE CategoryID = @CategoryID;
    
    SET @Fee = @BaseRate * @Hours;
    SET @Fee = @Fee + (@Fee * @PremiumPercent / 100);
    
    -- Discount for long parking (>8 hours)
    IF @Hours > 8
        SET @Fee = @Fee * 0.90;
    
    RETURN @Fee;
END;
GO

-- Function 2: get user total spending (Scalar)
CREATE FUNCTION fn_UserTotalSpending(@UserID INT)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @Total DECIMAL(10,2);
    SELECT @Total = ISNULL(SUM(t.Amount), 0)
    FROM Transactions t
    INNER JOIN ParkingRecords p ON t.RecordID = p.RecordID
    WHERE p.UserID = @UserID;
    RETURN @Total;
END;
GO

-- Function 3: table valued function to get active vahicles
CREATE FUNCTION fn_GetActiveVehicles()
RETURNS TABLE
AS
RETURN
(
    SELECT 
        ps.SlotNumber,
        ps.FloorNo,
        pr.PlateNumber,
        vt.TypeName AS VehicleType,
        pr.OwnerName,
        pr.EntryTime,
        DATEDIFF(HOUR, pr.EntryTime, GETDATE()) AS HoursParked
    FROM ParkingRecords pr
    INNER JOIN ParkingSlots ps ON pr.SlotID = ps.SlotID
    INNER JOIN VehicleTypes vt ON pr.VehicleTypeID = vt.VehicleTypeID
    WHERE pr.IsActive = 1
);
GO

-- Function 4: scaler function used to get daily revenue
CREATE FUNCTION fn_GetDailyRevenue(@Date DATE)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @Total DECIMAL(10,2);
    SELECT @Total = ISNULL(SUM(Amount), 0)
    FROM Transactions
    WHERE CAST(TransTime AS DATE) = @Date;
    RETURN @Total;
END;
GO

-- 6. Stored procedures 

-- Procedure 1: Register User
CREATE PROCEDURE sp_RegisterUser
    @FullName VARCHAR(100),
    @Email VARCHAR(100),
    @Phone VARCHAR(15),
    @CNIC VARCHAR(20),
    @Username VARCHAR(50),
    @Password VARCHAR(100)
AS
BEGIN
    IF EXISTS (SELECT 1 FROM Users WHERE Username = @Username)
    BEGIN
        SELECT 0 AS Result, 'Username exists' AS Message;
        RETURN;
    END
    
    IF EXISTS (SELECT 1 FROM Users WHERE Email = @Email)
    BEGIN
        SELECT 0 AS Result, 'Email exists' AS Message;
        RETURN;
    END
    
    INSERT INTO Users (RoleID, FullName, Email, Phone, CNIC, Username, UserPassword)
    VALUES (1, @FullName, @Email, @Phone, @CNIC, @Username, @Password);
    
    SELECT 1 AS Result, 'Registered Successfully' AS Message;
END;
GO

-- Procedure 2: Login User
CREATE PROCEDURE sp_LoginUser
    @Username VARCHAR(50),
    @Password VARCHAR(100)
AS
BEGIN
    UPDATE Users SET LastLoginDate = GETDATE()
    WHERE Username = @Username AND UserPassword = @Password AND IsActive = 1;
    
    SELECT UserID, FullName, Username, WalletBalance, RoleID
    FROM Users
    WHERE Username = @Username AND UserPassword = @Password AND IsActive = 1;
END;
GO

-- Procedure 3: Admin Login
CREATE PROCEDURE sp_AdminLogin
    @Username VARCHAR(50),
    @Password VARCHAR(100)
AS
BEGIN
    UPDATE Admin SET LastLogin = GETDATE()
    WHERE Username = @Username AND AdminPassword = @Password;
    
    SELECT AdminID, Username, FullName, Role
    FROM Admin
    WHERE Username = @Username AND AdminPassword = @Password;
END;
GO

-- Procedure 4: Get Free Slots
CREATE PROCEDURE sp_GetFreeSlots
AS
BEGIN
    SELECT SlotID, SlotNumber, FloorNo, sc.CategoryName
    FROM ParkingSlots ps
    INNER JOIN SlotCategories sc ON ps.CategoryID = sc.CategoryID
    WHERE ps.IsAvailable = 1 AND ps.IsActive = 1;
END;
GO

-- Procedure 5: Park Vehicle (With Transaction)
CREATE PROCEDURE sp_ParkVehicle
    @PlateNumber VARCHAR(20),
    @VehicleTypeName VARCHAR(20),
    @OwnerName VARCHAR(100),
    @OwnerCNIC VARCHAR(20),
    @Username VARCHAR(50) = NULL
AS
BEGIN
    BEGIN TRANSACTION;
    
    DECLARE @VehicleTypeID INT;
    SELECT @VehicleTypeID = VehicleTypeID FROM VehicleTypes WHERE TypeName = @VehicleTypeName;
    
    IF @VehicleTypeID IS NULL
    BEGIN
        ROLLBACK;
        SELECT 0 AS Result, 'Invalid vehicle type' AS Message;
        RETURN;
    END
    
    DECLARE @SlotID INT, @SlotNumber VARCHAR(5);
    
    SELECT TOP 1 @SlotID = SlotID, @SlotNumber = SlotNumber
    FROM ParkingSlots
    WHERE IsAvailable = 1 AND IsActive = 1
    ORDER BY FloorNo, SlotNumber;
    
    IF @SlotID IS NULL
    BEGIN
        ROLLBACK;
        SELECT 0 AS Result, 'No slots available' AS Message;
        RETURN;
    END
    
    DECLARE @UserID INT = NULL;
    IF @Username IS NOT NULL
        SELECT @UserID = UserID FROM Users WHERE Username = @Username AND IsActive = 1;
    
    INSERT INTO ParkingRecords (UserID, SlotID, PlateNumber, VehicleTypeID, OwnerName, OwnerCNIC)
    VALUES (@UserID, @SlotID, @PlateNumber, @VehicleTypeID, @OwnerName, @OwnerCNIC);
    
    UPDATE ParkingSlots SET IsAvailable = 0 WHERE SlotID = @SlotID;
    
    COMMIT TRANSACTION;
    
    SELECT 1 AS Result, 'Parked at ' + @SlotNumber AS Message, @SlotNumber AS SlotNumber;
END;
GO

-- Procedure 6: Exit Vehicle (With Transaction)
CREATE PROCEDURE sp_ExitVehicle
    @SlotNumber VARCHAR(5),
    @PaymentMethod VARCHAR(30) = 'Cash'
AS
BEGIN
    BEGIN TRANSACTION;
    
    DECLARE @RecordID INT, @SlotID INT, @VehicleTypeID INT, @CategoryID INT;
    DECLARE @EntryTime DATETIME, @Hours INT, @Fee DECIMAL(10,2);
    DECLARE @PaymentID INT;
    
    SELECT @PaymentID = PaymentID FROM PaymentMethods WHERE MethodName = @PaymentMethod;
    
    SELECT 
        @RecordID = pr.RecordID,
        @SlotID = pr.SlotID,
        @VehicleTypeID = pr.VehicleTypeID,
        @EntryTime = pr.EntryTime,
        @CategoryID = ps.CategoryID
    FROM ParkingRecords pr
    INNER JOIN ParkingSlots ps ON pr.SlotID = ps.SlotID
    WHERE ps.SlotNumber = @SlotNumber AND pr.IsActive = 1;
    
    IF @RecordID IS NULL
    BEGIN
        ROLLBACK;
        SELECT 0 AS Result, 'No vehicle found' AS Message;
        RETURN;
    END
    
    SET @Hours = DATEDIFF(HOUR, @EntryTime, GETDATE());
    IF @Hours < 1 SET @Hours = 1;
    
    SET @Fee = dbo.fn_CalculateFee(@VehicleTypeID, @Hours, @CategoryID);
    
    UPDATE ParkingRecords 
    SET ExitTime = GETDATE(), HoursParked = @Hours, TotalAmount = @Fee, IsActive = 0
    WHERE RecordID = @RecordID;
    
    INSERT INTO Transactions (RecordID, UserID, PaymentID, Amount)
    VALUES (@RecordID, (SELECT UserID FROM ParkingRecords WHERE RecordID = @RecordID), @PaymentID, @Fee);
    
    UPDATE ParkingSlots SET IsAvailable = 1 WHERE SlotID = @SlotID;
    
    COMMIT TRANSACTION;
    
    SELECT 1 AS Result, @Hours AS Hours, @Fee AS Amount;
END;
GO

-- Procedure 7: Dashboard Stats
CREATE PROCEDURE sp_GetDashboardStats
AS
BEGIN
    SELECT 
        (SELECT ISNULL(SUM(Amount), 0) FROM Transactions) AS TotalRevenue,
        (SELECT COUNT(*) FROM Users WHERE IsActive = 1) AS TotalUsers,
        (SELECT COUNT(*) FROM ParkingRecords WHERE IsActive = 1) AS ActiveVehicles,
        (SELECT COUNT(*) FROM ParkingRecords) AS TotalServed;
END;
GO

-- Procedure 8: Get All Users
CREATE PROCEDURE sp_GetAllUsers
AS
BEGIN
    SELECT UserID, FullName, Email, Phone, Username, WalletBalance, CreatedDate
    FROM Users WHERE IsActive = 1;
END;
GO

-- Procedure 9: Generate Report
CREATE PROCEDURE sp_GenerateReport
AS
BEGIN
    SELECT 
        GETDATE() AS ReportDate,
        (SELECT ISNULL(SUM(Amount), 0) FROM Transactions) AS TotalRevenue,
        (SELECT COUNT(*) FROM Users) AS TotalUsers,
        (SELECT COUNT(*) FROM ParkingRecords WHERE IsActive = 0) AS CompletedParkings;
END;
GO

-- Procedure 10: Get Vehicle Statistics
CREATE PROCEDURE sp_GetVehicleStats
AS
BEGIN
    SELECT 
        vt.TypeName,
        COUNT(pr.RecordID) AS TotalParkings,
        ISNULL(SUM(pr.TotalAmount), 0) AS TotalRevenue
    FROM VehicleTypes vt
    LEFT JOIN ParkingRecords pr ON vt.VehicleTypeID = pr.VehicleTypeID
    GROUP BY vt.TypeName;
END;
GO
---get available slots for user
CREATE PROCEDURE sp_GetAvailableSlotsForUser
    @VehicleTypeName VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @BaseRate DECIMAL(10,2);
    SELECT @BaseRate = BaseRate FROM VehicleTypes WHERE TypeName = @VehicleTypeName;
    
    SELECT 
        ps.SlotID,
        ps.SlotNumber,
        ps.FloorNo,
        sc.CategoryName,
        sc.PremiumPercent,
        @VehicleTypeName AS VehicleType,
        @BaseRate AS BaseRate,
        @BaseRate + (@BaseRate * sc.PremiumPercent / 100) AS TotalHourlyRate,
        ps.IsAvailable,
        CASE 
            WHEN ps.IsAvailable = 1 THEN 'FREE'
            ELSE 'OCCUPIED'
        END AS Status
    FROM ParkingSlots ps
    INNER JOIN SlotCategories sc ON ps.CategoryID = sc.CategoryID
    WHERE ps.IsAvailable = 1 AND ps.IsActive = 1
    ORDER BY ps.FloorNo, ps.SlotNumber;
END;
GO
--get single slot details
CREATE PROCEDURE sp_GetSlotDetails
    @SlotNumber VARCHAR(5),
    @VehicleTypeName VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @BaseRate DECIMAL(10,2);
    SELECT @BaseRate = BaseRate FROM VehicleTypes WHERE TypeName = @VehicleTypeName;
    
    SELECT 
        ps.SlotID,
        ps.SlotNumber,
        ps.FloorNo,
        sc.CategoryName,
        sc.PremiumPercent,
        @VehicleTypeName AS VehicleType,
        @BaseRate AS BaseRate,
        @BaseRate + (@BaseRate * sc.PremiumPercent / 100) AS TotalHourlyRate,
        ps.IsAvailable,
        CASE 
            WHEN ps.IsAvailable = 1 THEN 'FREE'
            ELSE 'OCCUPIED'
        END AS Status
    FROM ParkingSlots ps
    INNER JOIN SlotCategories sc ON ps.CategoryID = sc.CategoryID
    WHERE ps.SlotNumber = @SlotNumber;
END;
GO
--user select and park vahicle
CREATE PROCEDURE sp_UserSelectSlot
    @Username VARCHAR(50),
    @SlotNumber VARCHAR(5),
    @VehicleTypeName VARCHAR(20),
    @PlateNumber VARCHAR(20),
    @OwnerName VARCHAR(100),
    @OwnerCNIC VARCHAR(20)
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Check if slot exists and is free
        DECLARE @SlotID INT, @IsAvailable BIT;
        SELECT @SlotID = SlotID, @IsAvailable = IsAvailable
        FROM ParkingSlots
        WHERE SlotNumber = @SlotNumber AND IsActive = 1;
        
        IF @SlotID IS NULL
        BEGIN
            ROLLBACK;
            SELECT 0 AS Result, 'Slot not found' AS Message;
            RETURN;
        END
        
        IF @IsAvailable = 0
        BEGIN
            ROLLBACK;
            SELECT 0 AS Result, 'Slot is already occupied' AS Message;
            RETURN;
        END
        
        -- Get UserID
        DECLARE @UserID INT;
        SELECT @UserID = UserID FROM Users WHERE Username = @Username AND IsActive = 1;
        
        IF @UserID IS NULL
        BEGIN
            ROLLBACK;
            SELECT 0 AS Result, 'User not found' AS Message;
            RETURN;
        END
        
        -- Get VehicleTypeID
        DECLARE @VehicleTypeID INT;
        SELECT @VehicleTypeID = VehicleTypeID FROM VehicleTypes WHERE TypeName = @VehicleTypeName;
        
        -- Insert parking record
        INSERT INTO ParkingRecords (UserID, SlotID, PlateNumber, VehicleTypeID, OwnerName, OwnerCNIC, IsActive)
        VALUES (@UserID, @SlotID, @PlateNumber, @VehicleTypeID, @OwnerName, @OwnerCNIC, 1);
        
        -- Mark slot as occupied
        UPDATE ParkingSlots SET IsAvailable = 0 WHERE SlotID = @SlotID;
        
        COMMIT TRANSACTION;
        
        SELECT 1 AS Result, 'Vehicle parked at ' + @SlotNumber AS Message, @SlotNumber AS SlotNumber;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        SELECT 0 AS Result, ERROR_MESSAGE() AS Message;
    END CATCH
END;
GO

-- 7. TRIGGERS

-- Trigger 1: Auto calculate hours on exit
CREATE TRIGGER trg_CalcHours ON ParkingRecords
AFTER UPDATE
AS
BEGIN
    UPDATE pr
    SET HoursParked = DATEDIFF(HOUR, pr.EntryTime, pr.ExitTime)
    FROM ParkingRecords pr
    INNER JOIN inserted i ON pr.RecordID = i.RecordID
    WHERE i.ExitTime IS NOT NULL;
END;
GO

-- Trigger 2: Prevent negative wallet
CREATE TRIGGER trg_NoNegativeWallet ON Users
INSTEAD OF UPDATE
AS
BEGIN
    IF EXISTS (SELECT * FROM inserted WHERE WalletBalance < 0)
    BEGIN
        RAISERROR('Wallet balance cannot be negative!', 16, 1);
        RETURN;
    END
    
    UPDATE u 
    SET u.WalletBalance = i.WalletBalance,
        u.FullName = i.FullName,
        u.Email = i.Email,
        u.Phone = i.Phone,
        u.IsActive = i.IsActive
    FROM Users u 
    INNER JOIN inserted i ON u.UserID = i.UserID;
END;
GO

-- Trigger 3: Audit log for Users table
CREATE TRIGGER trg_AuditUsers
ON Users
AFTER UPDATE, DELETE
AS
BEGIN
    -- For UPDATE
    IF EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (TableName, ActionType, RecordID, OldValue, NewValue)
        SELECT 'Users', 'UPDATE', d.UserID,
               CONCAT('Wallet: ', d.WalletBalance, ', Active: ', d.IsActive),
               CONCAT('Wallet: ', i.WalletBalance, ', Active: ', i.IsActive)
        FROM deleted d
        INNER JOIN inserted i ON d.UserID = i.UserID
        WHERE d.WalletBalance != i.WalletBalance OR d.IsActive != i.IsActive;
    END
    
    -- For DELETE
    IF EXISTS (SELECT * FROM deleted) AND NOT EXISTS (SELECT * FROM inserted)
    BEGIN
        INSERT INTO AuditLog (TableName, ActionType, RecordID, OldValue)
        SELECT 'Users', 'DELETE', UserID, CONCAT('Name: ', FullName, ', Username: ', Username)
        FROM deleted;
    END
END;
GO

-- 8. VIEWS

-- View 1: Active Vehicles
CREATE VIEW vw_ActiveVehicles AS
SELECT ps.SlotNumber, ps.FloorNo, pr.PlateNumber, vt.TypeName AS VehicleType, pr.OwnerName, pr.EntryTime
FROM ParkingRecords pr
INNER JOIN ParkingSlots ps ON pr.SlotID = ps.SlotID
INNER JOIN VehicleTypes vt ON pr.VehicleTypeID = vt.VehicleTypeID
WHERE pr.IsActive = 1;
GO

-- View 2: Daily Revenue
CREATE VIEW vw_DailyRevenue AS
SELECT CAST(TransTime AS DATE) AS Date, COUNT(*) AS Transactions, SUM(Amount) AS Revenue
FROM Transactions
GROUP BY CAST(TransTime AS DATE);
GO
SELECT * FROM vw_DailyRevenue;


-- View 3: User Summary 
CREATE VIEW vw_UserSummary AS
SELECT 
    u.UserID, 
    u.FullName, 
    u.Username, 
    u.WalletBalance,
    ur.RoleName,
    COUNT(pr.RecordID) AS Parkings,
    ISNULL(SUM(pr.TotalAmount), 0) AS TotalSpent
FROM Users u
LEFT JOIN UserRoles ur ON u.RoleID = ur.RoleID
LEFT JOIN ParkingRecords pr ON u.UserID = pr.UserID
GROUP BY u.UserID, u.FullName, u.Username, u.WalletBalance, ur.RoleName;
GO
SELECT * FROM vw_UserSummary;


-- View 4: Slot Utilization
CREATE VIEW vw_SlotUtilization AS
SELECT 
    ps.SlotNumber,
    ps.FloorNo,
    sc.CategoryName,
    COUNT(pr.RecordID) AS TotalUses,
    ISNULL(SUM(pr.TotalAmount), 0) AS Revenue,
    CASE WHEN ps.IsAvailable = 1 THEN 'Free' ELSE 'Occupied' END AS Status
FROM ParkingSlots ps
LEFT JOIN ParkingRecords pr ON ps.SlotID = pr.SlotID
LEFT JOIN SlotCategories sc ON ps.CategoryID = sc.CategoryID
GROUP BY ps.SlotNumber, ps.FloorNo, sc.CategoryName, ps.IsAvailable;
GO
SELECT * FROM vw_SlotUtilization;


-- View 5: Monthly Revenue
CREATE VIEW vw_MonthlyRevenue AS
SELECT 
    YEAR(TransTime) AS Year, 
    MONTH(TransTime) AS Month,
    DATENAME(MONTH, TransTime) AS MonthName,
    COUNT(*) AS TotalTransactions,
    SUM(Amount) AS Revenue,
    AVG(Amount) AS AverageAmount
FROM Transactions
GROUP BY YEAR(TransTime), MONTH(TransTime), DATENAME(MONTH, TransTime);
GO
SELECT * FROM vw_MonthlyRevenue;

-- View 6: Complete Parking Records 
CREATE VIEW vw_CompleteParkingRecords AS
SELECT 
    pr.RecordID,
    pr.PlateNumber,
    vt.TypeName AS VehicleType,
    pr.OwnerName,
    ps.SlotNumber,
    sc.CategoryName AS SlotType,
    pr.EntryTime,
    pr.ExitTime,
    pr.HoursParked,
    pr.TotalAmount,
    t.Amount AS PaidAmount,
    pm.MethodName AS PaymentMethod,
    CASE WHEN pr.IsActive = 1 THEN 'Active' ELSE 'Completed' END AS Status,
    u.FullName AS CustomerName
FROM ParkingRecords pr
INNER JOIN VehicleTypes vt ON pr.VehicleTypeID = vt.VehicleTypeID
INNER JOIN ParkingSlots ps ON pr.SlotID = ps.SlotID
INNER JOIN SlotCategories sc ON ps.CategoryID = sc.CategoryID
LEFT JOIN Transactions t ON pr.RecordID = t.RecordID
LEFT JOIN PaymentMethods pm ON t.PaymentID = pm.PaymentID
LEFT JOIN Users u ON pr.UserID = u.UserID;
GO
SELECT * FROM vw_CompleteParkingRecords;
--User dashboard view
CREATE VIEW vw_UserDashboardSlots AS
SELECT 
    ps.SlotID,
    ps.SlotNumber,
    ps.FloorNo,
    sc.CategoryName,
    sc.PremiumPercent,
    ps.IsAvailable,
    CASE 
        WHEN ps.IsAvailable = 1 THEN 'FREE'
        ELSE 'OCCUPIED'
    END AS Status,
    CASE 
        WHEN sc.PremiumPercent = 0 THEN 'Regular'
        WHEN sc.PremiumPercent = 20 THEN 'Premium (+20%)'
        WHEN sc.PremiumPercent = 50 THEN 'VIP (+50%)'
        ELSE 'Special'
    END AS SlotTypeDescription
FROM ParkingSlots ps
INNER JOIN SlotCategories sc ON ps.CategoryID = sc.CategoryID
WHERE ps.IsActive = 1;
GO


--floor wise slots summary
CREATE VIEW vw_FloorWiseSlots AS
SELECT 
    ps.FloorNo,
    COUNT(ps.SlotID) AS TotalSlots,
    SUM(CASE WHEN ps.IsAvailable = 1 THEN 1 ELSE 0 END) AS FreeSlots,
    SUM(CASE WHEN ps.IsAvailable = 0 THEN 1 ELSE 0 END) AS OccupiedSlots
FROM ParkingSlots ps
WHERE ps.IsActive = 1
GROUP BY ps.FloorNo;
GO



-- Test 11: Views
SELECT * FROM vw_DailyRevenue;
SELECT * FROM vw_UserSummary;
SELECT * FROM vw_SlotUtilization;
SELECT * FROM vw_MonthlyRevenue;
SELECT * FROM vw_CompleteParkingRecords;
GO


-- Test 1: Check Normalization by showing lookup tables
SELECT 'Vehicle Types' AS TableName, * FROM VehicleTypes;
SELECT 'Slot Categories' AS TableName, * FROM SlotCategories;
SELECT 'Payment Methods ' AS TableName, * FROM PaymentMethods;
SELECT 'User Roles ' AS TableName, * FROM UserRoles;
GO

-- Test 2: Register User
EXEC sp_RegisterUser 'Normalized User', 'normal@test.com', '03123456789', '88888-8888888', 'normuser', 'pass123';
GO

-- Test 3: Login
EXEC sp_LoginUser 'normuser', 'pass123';
GO

-- Test 4: Get Free Slots
EXEC sp_GetFreeSlots;
GO

-- Test 5: Park Vehicle
EXEC sp_ParkVehicle 'NORM-123', 'Car', 'Normalized User', '88888-8888888', 'normuser';
GO

-- Test 6: Check Active Vehicles (Function)
SELECT * FROM fn_GetActiveVehicles();
GO

-- Test 7: Check Active Vehicles (View)
SELECT * FROM vw_ActiveVehicles;
GO

-- Test 8: Dashboard Stats
EXEC sp_GetDashboardStats;
GO

-- Test 9: Get All Users
EXEC sp_GetAllUsers;
GO

-- Test 10: Vehicle Stats
EXEC sp_GetVehicleStats;
GO

-- Test 11: Views
SELECT * FROM vw_DailyRevenue;
SELECT * FROM vw_UserSummary;
SELECT * FROM vw_SlotUtilization;
SELECT * FROM vw_MonthlyRevenue;
SELECT * FROM vw_CompleteParkingRecords;
GO


-- Test 1: Check all free slots for Car
EXEC sp_GetAvailableSlotsForUser 'Car';
GO

-- Test 2: Check specific slot details
EXEC sp_GetSlotDetails 'S03', 'Car';
GO

-- Test 3: Check floor wise summary
SELECT * FROM vw_FloorWiseSlots;
GO

-- Test 4: Check user dashboard view
SELECT * FROM vw_UserDashboardSlots;
GO

-- Test 5: User selects and parks vehicle (pehle se registered user ke liye)
EXEC sp_UserSelectSlot 'nimra123', 'S03', 'Car', 'LEH-1234', 'Nimra', '12345-6789012-3';
GO

-- Test 6: Verify slot is now occupied
SELECT * FROM vw_UserDashboardSlots WHERE SlotNumber = 'S03';
GO
SELECT name FROM sys.databases WHERE name = 'MyParkingDB';
SELECT * FROM Users;
USE MyParkingDB;
EXEC sp_GetAllUsers;