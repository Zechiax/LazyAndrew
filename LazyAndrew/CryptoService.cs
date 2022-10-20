using System.Security.Cryptography;

namespace LazyAndrew;

public class CryptoService
{
    private SHA1 _sha1;
    private SHA512 _sha512;
    
    public CryptoService()
    {
        _sha1 = SHA1.Create();
        _sha512 = SHA512.Create();
    }

    public string ComputeFileHash(FileStream stream, Enums.HashAlgorithm algorithm = Enums.HashAlgorithm.sha512)
    {
        return algorithm switch
        {
            Enums.HashAlgorithm.sha1 => Convert.ToHexString(_sha1.ComputeHash(stream)),
            Enums.HashAlgorithm.sha512 => Convert.ToHexString(_sha512.ComputeHash(stream)),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, null)
        };
    }
}