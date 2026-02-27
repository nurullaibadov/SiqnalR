using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Features.Conversations.DTOs;
using UpAllNight.Domain.Entities;

namespace UpAllNight.Application.Mappings
{
    public class ConversationMappingProfile : Profile
    {
        public ConversationMappingProfile()
        {
            CreateMap<Conversation, ConversationDto>();
        }
    }
}
