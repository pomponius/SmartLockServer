CREATE TABLE [dbo].[Table_User]
(
	[UserId] INT NOT NULL PRIMARY KEY, 
    [UserName] NVARCHAR(30) NOT NULL, 
    [UserSurname] NVARCHAR(30) NOT NULL, 
    [UserAddress] NVARCHAR(60) NULL, 
    [UserCity] NVARCHAR(15) NULL, 
    [UserRegion] NVARCHAR(15) NULL, 
    [UserPostalCode] NVARCHAR(10) NULL, 
    [UserCountry] NVARCHAR(15) NULL, 
    [UserPhone] NVARCHAR(24) NULL, 
    [UserMail] NVARCHAR(50) NOT NULL, 
    [UserCardType] NVARCHAR(50) NOT NULL, 
    [UserCardID] NVARCHAR(20) NOT NULL, 
    [UserCardExpire] DATETIME NOT NULL, 
    [UserPin] NCHAR(5) NOT NULL, 
    [UserPinExpire] DATETIME NOT NULL, 
    [UserRegistrationDate] DATETIME NOT NULL, 
    [UserLastAccess] DATETIME NULL
)
