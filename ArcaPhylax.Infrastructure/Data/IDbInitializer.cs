namespace ArcaPhylax.Data;

public interface IDbInitializer
{
    void EnsureCreatedAndSeed();
}