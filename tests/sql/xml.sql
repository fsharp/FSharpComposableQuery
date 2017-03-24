DROP TABLE IF EXISTS [Data];
CREATE TABLE [Data] (
    [Name]   VARCHAR(255) NOT NULL,
    [ID]     INT          NOT NULL,
    [Entry]  INT          NOT NULL,
    [Pre]    INT          NOT NULL,
    [Post]   INT          NOT NULL,
    [Parent] INT          NOT NULL,
    PRIMARY KEY ([Id] ASC)
);

DROP TABLE IF EXISTS [Attribute];
CREATE TABLE [Attribute] (
    [Element] INT          NOT NULL,
    [Name]    VARCHAR(255) NOT NULL,
    [Value]   VARCHAR(255) NOT NULL,
    PRIMARY KEY ([Element],[Name] ASC)
);

DROP TABLE IF EXISTS [Text];
CREATE TABLE [Text] (
    [ID]      INT          NOT NULL,
    [Value]   VARCHAR(255) NOT NULL,
    PRIMARY KEY ([ID] ASC)
);