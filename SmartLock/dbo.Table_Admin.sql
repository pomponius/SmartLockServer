CREATE TABLE [dbo].[Table]
(
	[AdminID] INT NOT NULL PRIMARY KEY, 
    [AdminName] NVARCHAR(30) NOT NULL, 
    [AdminSurname] NVARCHAR(30) NOT NULL, 
    [AdminLogin] NVARCHAR(30) NOT NULL, 
    [AdminPassword] NVARCHAR(30) NOT NULL, 
    [AdminRegistrationDate] DATETIME NOT NULL
)
