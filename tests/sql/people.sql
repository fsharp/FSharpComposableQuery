DROP TABLE IF EXISTS People;
CREATE TABLE People (
    [Name] VARCHAR(255) NOT NULL,
    [Age]   INT           NOT NULL,
    PRIMARY KEY ([Name],[Age] ASC)
);

DROP TABLE IF EXISTS Couples;
CREATE TABLE Couples (
    [Her] VARCHAR(255) NOT NULL,
    [Him] VARCHAR(255) NOT NULL,
    PRIMARY KEY ([Her],[Him] ASC)
);