using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Features.Conversations.DTOs;
using UpAllNight.Application.Features.Users.DTOs;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Mappings
{
    public class UserMappingProfile : Profile
    {
        public UserMappingProfile()
        {
            CreateMap<User, UserDto>();
            CreateMap<User, UserProfileDto>();
            CreateMap<ConversationParticipant, ParticipantDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName))
                .ForMember(d => d.ProfilePictureUrl, o => o.MapFrom(s => s.User.ProfilePictureUrl))
                .ForMember(d => d.IsOnline, o => o.MapFrom(s => s.User.IsOnline))
                .ForMember(d => d.LastSeenAt, o => o.MapFrom(s => s.User.LastSeenAt));
        }
    }
}
