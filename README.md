# Sculptor

<p align="left">
  <a href="https://github.com/cleberMargarida/sculptor/actions/workflows/workflow.yml">
    <img src="https://github.com/cleberMargarida/sculptor/actions/workflows/workflow.yml/badge.svg" alt="Build-deploy pipeline">
  </a>
  <a href="https://www.nuget.org/packages/Sculptor.Core">
    <img src="https://img.shields.io/nuget/vpre/Sculptor.Core.svg" alt="EasyChain Nuget Version">
  </a>  
</p>

## Overview
A lightweight library designed to facilitate Domain-Driven Design (DDD) with rich models. 
Includes foundational classes like `AggregateRoot`, `RichModel`, `ValueObject`, `Entity`, and `Result` to help structure robust, maintainable, and expressive domain models. 
Perfect for developers seeking to embrace DDD principles with ease.

It supports multiple .NET versions including .NET Core 2.1, .NET Core 3.1, .NET 5, .NET 6, .NET 7, .NET 8, and .NET 9.

## Features
- Easy-to-use API for DDD modeling
- Source generator for value objects equality, hash code
- Support for ServiceProvider anywhere in the domain model

## Getting Started

### Installation
To install Sculptor, run the following command in your terminal:

```bash
dotnet add package Sculptor.AspNet
```

### Usage
Here is a simple example of how to use Sculptor in your project:

In program file, add the following code:
```csharp
app.UseSculptor();
```
The provided code includes a custom middleware that sets the static `ServiceProvider` property 
of the `ServiceProviderAccessor` class to the `RequestServices` of the current `HttpContext`.
This enables access to the dependency injection container for the current HTTP request within 
domain classes such as `AggregateRoot`, `Entity`, and `ValueObject` through the `Services` property.

By leveraging this approach, you can access domain services from anywhere within your domain model, 
enabling the creation of rich, expressive models while ensuring that business rules are maintained 
consistently within the domain.

#### `BankAccount` Class

This class serves as the **Aggregate Root**, encapsulating the main business rules and operations for a bank account.
```csharp
using Sculptor.Core;

public class BankAccount : AggregateRoot
{
    public AccountOwner Owner { get; private set; }
    public Money Balance { get; private set; }

    // empty constructor for serialization and EF core
    private BankAccount() { }

    // Private constructor ensures controlled object creation through the Create method.
    private BankAccount(AccountOwner owner, Money initialBalance)
    {
        Owner = owner;
        Balance = initialBalance;
    }

    // Result Pattern Over Exceptions
    public static Result<BankAccount> Create(AccountOwner owner, Money initialBalance, IServiceProvider serviceProvider)
    {
        // Access the DbContext from the ServiceProvider
        var isExistingAccount = serviceProvider.GetRequiredService<BankAccountDbContext>()
            .BankAccounts.Any(account => account.Owner.Name == owner.Name);

        if (isExistingAccount)
            return Result<BankAccount>.Fail("An existing bank account already exists for this owner.");

        if (owner == null)
            return Result<BankAccount>.Fail("Account owner is required.");

        if (initialBalance == null || initialBalance.Amount < 0)
            return Result<BankAccount>.Fail("Initial balance must be a non-negative value.");

        return Result<BankAccount>.Ok(new BankAccount(owner, initialBalance));
    }

    // Business operation: Deposit money into the account.
    public Result Deposit(Money amount)
    {
        if (amount == null || amount.Amount <= 0)
            return Result.Fail("Deposit amount must be greater than zero.");

        Balance.Add(amount);

        return Result.Ok();
    }

    // Business operation: Withdraw money from the account.
    public Result Withdraw(Money amount)
    {
        if (amount == null || amount.Amount <= 0)
            return Result.Fail("Withdrawal amount must be greater than zero.");

        Balance = Balance.Subtract(amount);

        return Result.Ok();
    }
}
```

#### `AccountOwner` Class

This class is an **Entity** that represents the account owner, identified by unique attributes such as name and email.
- **`Entity` Base Class**: The `AccountOwner` inherits from the `Entity` class, which gives it an identity and provides basic operations like equality comparison and hash code generation.
  
```csharp
using Sculptor.Core;

public class AccountOwner : Entity
{
    public string Name { get; private set; }
    public string Email { get; private set; }

    // empty constructor for serialization and EF core
    private BankAccount() { }

    // Private constructor ensures controlled object creation through the Create method.
    private AccountOwner(string name, string email)
    {
        Name = name; // Set the owner's name.
        Email = email; // Set the owner's email.
    }

    public static Result<AccountOwner> Create(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<AccountOwner>.Fail("Name is required.");

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            return Result<AccountOwner>.Fail("A valid email is required.");

        return Result<AccountOwner>.Ok(new AccountOwner(name, email));
    }
}
```

#### `Email` Class

This class is a **Value Object** that encapsulates an email address. It demonstrates immutability and validation logic.
- **Partial Class**: The `Email` class is defined as `partial` to support **source generators** for automatically generating equality and hash code methods. These features help maintain consistency and ensure that value objects behave correctly in collections or when compared.

```csharp
using Sculptor.Core;

public partial class Email : ValueObject
{
    public string Value { get; }

    private Email() { }

    public Email(string value)
    {
        Value = value;
    }
}
```

### SourceGeneration
`ValueObject` must be a `partial` class to allow source generators to add equality and hash code calculation automatically.

### Setting the Service Provider Manually

To set the service provider without relying on built-in mechanisms, like in `BackgroundService`'s, messaging systems, you can manually assign the `IServiceProvider` to the static `ServiceProvider` property of `ServiceProviderAccessor` like this:

```csharp
ServiceProviderAccessor.ServiceProvider = context.RequestServices;
```

### Key Features of the Code
- **Domain Classes**: `AggregateRoot`, `Entity` and `ValueObjects`. These base classes provide foundational behaviors.
- **ServiceProvider**: The `Create` method in `BankAccount` uses `IServiceProvider` to access the `DbContext` to check if an account for the provided `owner` already exists.
- **Result Pattern**: The `Result<BankAccount>` pattern is used instead of exceptions. The `Create` method returns either a success result (`Result.Ok`) or a failure result (`Result.Fail`), making it easier to handle errors in the domain model.

## License
Sculptor is licensed under the MIT License. See the [LICENSE](LICENSE) file for more details.
