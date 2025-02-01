using WeddingShare.Helpers;

namespace WeddingShare.Configurations
{
    internal static class DependencyInjectionConfiguration
    {
        public static void AddDependencyInjectionConfiguration(this IServiceCollection services)
        {
            services.AddSingleton<IConfigHelper, ConfigHelper>();
            services.AddSingleton<IEnvironmentWrapper, EnvironmentWrapper>();
            services.AddSingleton<IGalleryHelper, GalleryHelper>();
            services.AddSingleton<IImageHelper, ImageHelper>();
            services.AddSingleton<IFileHelper, FileHelper>();
            services.AddSingleton<IDeviceDetector, DeviceDetector>();
            services.AddSingleton<ISmtpClientWrapper, SmtpClientWrapper>();
            services.AddSingleton<IEncryptionHelper, EncryptionHelper>();
            services.AddSingleton<IUrlHelper, UrlHelper>();
        }
    }
}