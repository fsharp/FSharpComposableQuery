DROP TABLE IF EXISTS Departments;
CREATE TABLE Departments (
    [Dpt] VARCHAR(255) NOT NULL,
    PRIMARY KEY ([Dpt] ASC)
);

DROP TABLE IF EXISTS Employees;
CREATE TABLE Employees (
    [Emp]     VARCHAR(255) NOT NULL,
    [Dpt]     VARCHAR(255) NOT NULL,
    [Salary]  INT          NOT NULL,
    PRIMARY KEY ([Dpt],[Emp] ASC)
);

DROP TABLE IF EXISTS Contacts;
CREATE TABLE Contacts (
    [Dpt]     VARCHAR(255) NOT NULL,
    [Contact] VARCHAR(255) NOT NULL,
    [Client]  INT		   NOT NULL,
    PRIMARY KEY ([Dpt],[Contact] ASC)
);

DROP TABLE IF EXISTS Tasks;
CREATE TABLE Tasks (
    [Emp] VARCHAR(255) NOT NULL,
    [Tsk] VARCHAR(255) NOT NULL,
    PRIMARY KEY ([Tsk],[Emp] ASC)
);
