using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Features.Messages.DTOs;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Mappings
{
    public class MessageMappingProfile : Profile
    {
        public MessageMappingProfile()
        {
            CreateMap<Message, MessageDto>()
                .ForMember(d => d.SenderUserName, o => o.MapFrom(s => s.Sender.UserName))
                .ForMember(d => d.SenderProfilePicture, o => o.MapFrom(s => s.Sender.ProfilePictureUrl));

            CreateMap<MessageAttachment, AttachmentDto>();
            CreateMap<MessageReaction, ReactionDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.UserName));
        }
    }
}
