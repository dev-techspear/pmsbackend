namespace PMS.Application.Common.Interfaces;

/// <summary>Reversible encryption for project credentials. Plaintext never reaches the database.</summary>
public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string payload);
}
