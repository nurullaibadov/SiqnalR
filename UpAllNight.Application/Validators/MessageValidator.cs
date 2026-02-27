using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Features.Messages.DTOs;

namespace UpAllNight.Application.Validators
{
    public class SendMessageRequestValidator : AbstractValidator<SendMessageRequestDto>
    {
        public SendMessageRequestValidator()
        {
            RuleFor(x => x.ConversationId)
                .NotEmpty().WithMessage("ConversationId is required");

            When(x => x.Type == MessageType.Text, () =>
            {
                RuleFor(x => x.Content)
                    .NotEmpty().WithMessage("Content is required for text messages")
                    .MaximumLength(5000).WithMessage("Message cannot exceed 5000 characters");
            });
        }
    }
}
