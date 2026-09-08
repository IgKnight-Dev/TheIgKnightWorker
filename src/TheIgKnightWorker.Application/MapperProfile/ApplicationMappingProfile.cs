using AutoMapper;
using TheIgKnightWorker.Application.DTOs;
using TheIgKnightWorker.Domain.Entities;

namespace TheIgKnightWorker.Application.MapperProfile;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {

        this.CreatePortfolioMappings();
    }

    
    private void CreatePortfolioMappings()
    {
        this.CreateMap<Portfolio, CreatePortfolioDto>().ReverseMap();
    }
}