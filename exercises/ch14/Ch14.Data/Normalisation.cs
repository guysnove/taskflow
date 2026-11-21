namespace Ch14.Data;

public static class Normalisation
{
    // NOT normalised: the customer's email is copied into every order.
    public const string Flat = """
        CREATE TABLE Orders (
            Id            INTEGER PRIMARY KEY,
            Customer      TEXT,
            CustomerEmail TEXT,
            Product       TEXT,
            Qty           INTEGER);
        INSERT INTO Orders (Customer, CustomerEmail, Product, Qty) VALUES
            ('Ana', 'ana@example.com', 'Notebook', 2),
            ('Ana', 'ana@example.com', 'Pen',      5),
            ('Ana', 'ana@example.com', 'Lamp',     1);
        """;

    // Normalised: each fact stored once. Orders point at the customer.
    public const string Normal = """
        CREATE TABLE Customers (
            Id    INTEGER PRIMARY KEY,
            Name  TEXT NOT NULL,
            Email TEXT NOT NULL UNIQUE);
        CREATE TABLE Orders (
            Id         INTEGER PRIMARY KEY,
            CustomerId INTEGER NOT NULL REFERENCES Customers(Id),
            Product    TEXT NOT NULL,
            Qty        INTEGER NOT NULL CHECK (Qty > 0));
        INSERT INTO Customers (Name, Email) VALUES ('Ana', 'ana@example.com');
        INSERT INTO Orders (CustomerId, Product, Qty) VALUES (1, 'Notebook', 2), (1, 'Pen', 5), (1, 'Lamp', 1);
        """;
}
