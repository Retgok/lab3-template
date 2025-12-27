using Microsoft.EntityFrameworkCore;

namespace FlightService;

public class FlightRepo : IFlightRepo
{
    private readonly FlightDb _db;
    public FlightRepo(FlightDb db) { _db = db; }


    public async Task<List<Flight>> GetAllAsync(int page, int size)
    {
        return await _db.Flights
            .Include(f => f.FromAirport)
            .Include(f => f.ToAirport)
            .OrderBy(f => f.Id)
            .Skip((page-1) * size)
            .Take(size)
            .ToListAsync();
    }


    public async Task<Flight?> GetByFlightNumberAsync(string flightNumber)
    {
        return await _db.Flights
            .Include(f => f.FromAirport)
            .Include(f => f.ToAirport)
            .FirstOrDefaultAsync(f => f.FlightNumber == flightNumber);
    }
}