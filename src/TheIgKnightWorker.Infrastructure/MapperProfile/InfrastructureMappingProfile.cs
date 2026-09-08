using Newtonsoft.Json;
using TheIgKnightWorker.Infrastructure.DbModels;
using static TheIgKnightWorker.Domain.Enums.Enums;

namespace TheIgKnightWorker.Infrastructure.MapperProfile;

public class InfrastructureMappingProfile : Profile
{
    public InfrastructureMappingProfile()
    {

        this.CreateUserMapping();

        this.CreateSkillMapping();

        // Document mapping
        this.CreateDocumentMapping();

        this.CreateEventMapping();
    }




    private void CreateUserMapping()
    {
        this.CreateMap<User, DbUser>()
        .ForMember(dest => dest.Privileges,
                   opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Privileges)))
        .ForMember(dest => dest.Features,
                   opt => opt.MapFrom(src => JsonConvert.SerializeObject(src.Features)))
        .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
        .ReverseMap()
        .ForMember(dest => dest.Privileges,
                   opt => opt.MapFrom(src => JsonConvert.DeserializeObject<List<string>>(src.Privileges)))
        .ForMember(dest => dest.Features,
                   opt => opt.MapFrom(src => JsonConvert.DeserializeObject<List<string>>(src.Features)));
    }

    private void CreateSkillMapping()
    {
        this.CreateMap<Skill, DbSkill>()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (short)src.Category))
            .ReverseMap()
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => (SkillCategory)src.Category));
    }

    private void CreateEventMapping()
    {
        this.CreateMap<Event, DbEvent>()
            .ForMember(
                dest => dest.EventType,
                opt => opt.MapFrom(src => (short)src.EventType))
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(src => (short)src.Status))
            .ReverseMap()
            .ForMember(
                dest => dest.EventType,
                opt => opt.MapFrom(src => (EventType)src.EventType))
            .ForMember(
                dest => dest.Status,
                opt => opt.MapFrom(src => (EventStatus)src.Status));
    }

    // New: add mapping for Document <-> DbDocument
    private void CreateDocumentMapping()
    {
        this.CreateMap<Document, DbDocument>().ReverseMap();
    }
}

public class PortfolioMetaDataConverter
    : IValueConverter<string, PortfolioMetaData>
{
    public PortfolioMetaData Convert(
        string sourceMember,
        ResolutionContext context)
    {
        var settings =
            new JsonSerializerSettings
            {
                Error = (sender, args) =>
                {
                    args.ErrorContext.Handled = true;
                }
            };

        var meta =
            JsonConvert.DeserializeObject<PortfolioMetaData>(
                sourceMember,
                settings
            );

        if (meta == null)
        {
            return new PortfolioMetaData();
        }

        meta.BasicInfo ??= new BasicInfo();

        meta.BasicInfo.Links ??=
            new List<PortfolioLink>();

        return meta;
    }
}

