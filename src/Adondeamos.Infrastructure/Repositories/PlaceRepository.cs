using Adondeamos.Application.Abstractions;
using Adondeamos.Domain.Entities;
using Adondeamos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adondeamos.Infrastructure.Repositories;

public sealed class PlaceRepository : IPlaceRepository
{
    private readonly AdondeamosDbContext _context;

    public PlaceRepository(AdondeamosDbContext context) => _context = context;

    public void Add(Place place) => _context.Places.Add(place);

    public Task<Place?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Places.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<Place?> GetByGooglePlaceIdAsync(string googlePlaceId, CancellationToken cancellationToken = default) =>
        _context.Places.FirstOrDefaultAsync(p => p.GooglePlaceId == googlePlaceId, cancellationToken);
}
