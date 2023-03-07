// See https://aka.ms/new-console-template for more information

using System.Data.SQLite;
using ConsoleTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

var data = new List<LoanCheckResult>();

var fillingFormProgress = new Dictionary<FillingFormPart, bool>();
var exitRequested = false;

Console.WriteLine("Hello, welcome to credit assessment program, press any key to begin evaluation process or press R for up to date reports, alternatively press X to exit");

while (!exitRequested)
{
    
    fillingFormProgress = new Dictionary<FillingFormPart, bool>()
    {
        { FillingFormPart.AssetValue, false },
        { FillingFormPart.CreditScore, false },
        { FillingFormPart.LoanAmount, false }
    };
    
    var key = Console.ReadKey();

    switch (key.KeyChar)
    {
        case 'r' or 'R' when data.Count == 0:
            Console.WriteLine("\r\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("No reports available.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press 1 to run eligibility process or X to exit.");
            continue;
        case 'r' or 'R':
        {
            Console.WriteLine("\r\n");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Detailed report of all eligibility checks performed");
            Console.WriteLine("\r\n");
            Console.ForegroundColor = ConsoleColor.Cyan;
            var table = new ConsoleTable("Loan Amount", "Asset Value", "Credit Score", "LTV", "Success");
            foreach (var row in data)
            {
                table.AddRow(row.LoanAmount, row.AssetValue, row.CreditScore, row.CalculateLTV().ToString("0.00"), row.Success);
            }
            table.Write();
            
            var table2 = new ConsoleTable("Total Number of Applications", "Total Successful", "Total Not Eligible", "Mean Average LTV of All Applications");
            table2.AddRow(data.Count, data.Count(x => x.Success), data.Count(x => !x.Success), data.Average(x => x.CalculateLTV()).ToString("0.00"));
            
            table2.Write();
            
            Console.WriteLine("Press X to exit, or any other key to run eligibility process");
            Console.ForegroundColor = ConsoleColor.White;
            continue;
        }
        case 'x' or 'X':
            exitRequested = true;
            continue;
    }

    Console.WriteLine("\r\n");

    decimal loanAmount = 0;
    decimal assetValue = 0;
    var creditScore = 0;

    while (!fillingFormProgress[FillingFormPart.LoanAmount])
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Please type in required loan value (GBP):");

        var loanAmountInput = Console.ReadLine();

        if (!decimal.TryParse(loanAmountInput, out loanAmount))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Wrong value, loan value must be numeric.");
            continue;
        }

        fillingFormProgress[FillingFormPart.LoanAmount] = true;
    }

    while (!fillingFormProgress[FillingFormPart.AssetValue]) 
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Please type in asset value that the loan will be secured against (GBP):");
        
        var assetValueInput = Console.ReadLine();

        if (!decimal.TryParse(assetValueInput, out assetValue))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Wrong value, asset value must be numeric.");
            continue;
        }

        fillingFormProgress[FillingFormPart.AssetValue] = true;
    }

    while (!fillingFormProgress[FillingFormPart.CreditScore])
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Please type in applicant's credit score (1 to 999):");
        
        var creditScoreInput = Console.ReadLine();

        if (!int.TryParse(creditScoreInput, out creditScore))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Wrong value, credit score must be numeric.");
            continue;
        }
        
        if (creditScore is < 1 or > 999)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Wrong value, credit score must be between 1 and 999 inclusive.");
            continue;   
        }

        fillingFormProgress[FillingFormPart.CreditScore] = true;
    }

    var loanCheckRequest = new LoanCheckRequest()
    {
        LoanAmount = loanAmount,
        AssetValue = assetValue,
        CreditScore = creditScore
    };

    var success = loanCheckRequest.IsEligible();

    var loanCheckResult = LoanCheckResult.FromLoanCheckRequest(loanCheckRequest, success);

    data.Add(loanCheckResult);

    switch (success)
    {
        case true:
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Application is eligible for loan with LTV:{loanCheckRequest.CalculateLTV():0.00}");
            break;
        case false:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Unfortunately application doesn't meet eligibility criteria, LTV: {loanCheckRequest.CalculateLTV():0.00}");
            break;
    }

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("Press X to exit, R to display reports, or any other key to run eligibility process again");
}

Console.WriteLine("\r\n");
Console.WriteLine("Thank you for using our app and have a nice day");
Console.WriteLine("Press any key to exit");
Console.ReadKey();

public interface ILoanCheckRequest
{
    public decimal LoanAmount { get; set; }
    public decimal AssetValue { get; set; }
    public int CreditScore { get; set; }
}

public class LoanCheckRequest: ILoanCheckRequest
{
    public decimal LoanAmount { get; set; }
    public decimal AssetValue { get; set; }
    public int CreditScore { get; set; }
}

public class LoanCheckResult: LoanCheckRequest
{
    public static LoanCheckResult FromLoanCheckRequest(LoanCheckRequest request, bool success)
    {
        return new LoanCheckResult()
        {
            AssetValue = request.AssetValue,
            LoanAmount = request.LoanAmount,
            CreditScore = request.CreditScore,
            Success = success
        };
    }

    public bool Success { get; set; }
}

public static class LoanCheckExtensions
{
    public static bool IsEligible(this LoanCheckRequest loanCheckRequest)
    {
        switch (loanCheckRequest.LoanAmount)
        {
            case < 100000 or > 15000000:
                return false;
            case >= 1000000:
                return loanCheckRequest.CalculateLTV() < 60 && loanCheckRequest.CreditScore >= 950;
            case < 1000000:
                if (loanCheckRequest.CalculateLTV() < 60 )
                {
                    return loanCheckRequest.CreditScore >= 750;
                }

                if (loanCheckRequest.CalculateLTV() is > 60 and < 80)
                {
                    return loanCheckRequest.CreditScore >= 800;
                }

                if (loanCheckRequest.CalculateLTV() is > 80 and < 90)
                {
                    return loanCheckRequest.CreditScore >= 900;
                }

                return !(loanCheckRequest.CalculateLTV() > 90); 
            
             // Based on this 
             //   If the LTV is less than 60%, the credit score of the applicant must be 750 or more
             //   If the LTV is less than 80%, the credit score of the applicant must be 800 or more
             //   If the LTV is less than 90%, the credit score of the applicant must be 900 or more
             //   If the LTV is 90% or more, the application must be declined
            
            
            // I have made an assumption here that LTV and credit sore is calculated based on range where LTV falls in for example between 60 and 80 or between 80 and 90;
            // Given that LTV of below 60, for example 40% would technically apply to all conditions, 
            // which means check for 40% would be true at every step so it would be hard to determine what credit score is required for it if all 3 would apply.
            // Obviously outcome boolean determining whether application is eligible would be valid and same for all conditions, however if we would like to add more reporting on why eligibility check failed,
            // it would be hard to do so if all three options would return true
            
        }
    }

    public static decimal CalculateLTV(this ILoanCheckRequest loanCheckRequest)
    {
        return (loanCheckRequest.LoanAmount / loanCheckRequest.AssetValue) * 100;
    }
}

public enum FillingFormPart
{
    LoanAmount,
    AssetValue,
    CreditScore
};