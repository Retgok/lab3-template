namespace FlightService;


public interface IFlightRepo
{
    Task<List<Flight>> GetAllAsync(int page, int size);
    Task<Flight?> GetByFlightNumberAsync(string flightNumber);
}