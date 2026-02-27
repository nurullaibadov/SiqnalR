using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UpAllNight.Application.Features.Admin.Services;
using UpAllNight.Application.Features.Auth.Services;
using UpAllNight.Application.Features.Conversations.Services;
using UpAllNight.Application.Features.Messages.Services;
using UpAllNight.Application.Features.Notifications.Services;
using UpAllNight.Application.Features.Users.Services;
using UpAllNight.Application.Interfaces.Services;

namespace UpAllNight.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            // Services
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IConversationService, ConversationService>();
            services.AddScoped<IMessageService, MessageService>();
            services.AddScoped<INotificationAppService, NotificationAppService>();
            services.AddScoped<IAdminService, AdminService>();

            return services;
        }
    }
}
