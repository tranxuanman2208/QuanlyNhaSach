namespace QuanlyNhaSach.Services
{
    /// <summary>
    /// Backward-compatible service alias inheriting directly from <see cref="MockDataStore"/>.
    /// Ensures Customer, Staff, and Manager portals share one single in-memory store instance
    /// without duplicating any collections.
    /// </summary>
    public class MockDataService : MockDataStore
    {
    }
}
