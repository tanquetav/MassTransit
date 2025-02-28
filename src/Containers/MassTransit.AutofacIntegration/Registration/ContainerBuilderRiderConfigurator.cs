namespace MassTransit.AutofacIntegration.Registration
{
    using System;
    using System.Collections.Generic;
    using Autofac;
    using MassTransit.Registration;
    using Riders;


    public class ContainerBuilderRiderConfigurator :
        RegistrationConfigurator,
        IContainerBuilderRiderConfigurator
    {
        readonly HashSet<Type> _riderTypes;
        protected readonly RegistrationCache<object> Registrations;

        public ContainerBuilderRiderConfigurator(ContainerBuilder builder, IContainerRegistrar registrar, HashSet<Type> riderTypes)
            : base(registrar)
        {
            Builder = builder;
            _riderTypes = riderTypes;
            Registrations = new RegistrationCache<object>();
        }

        public ContainerBuilder Builder { get; }

        public void AddRegistration<T>(T registration)
            where T : class
        {
            Registrations.GetOrAdd(typeof(T), _ => registration);
        }

        public virtual void SetRiderFactory<TRider>(IRegistrationRiderFactory<TRider> riderFactory)
            where TRider : class, IRider
        {
            if (riderFactory == null)
                throw new ArgumentNullException(nameof(riderFactory));

            ThrowIfAlreadyConfigured<TRider>();

            IRiderRegistrationContext CreateRegistrationContext(IComponentContext context)
            {
                var registration = CreateRegistration(context.Resolve<IConfigurationServiceProvider>());
                return new RiderRegistrationContext(registration, Registrations);
            }

            var registrationKey = typeof(TRider).Name;

            Builder.Register(CreateRegistrationContext)
                .Keyed<IRiderRegistrationContext>(registrationKey)
                .SingleInstance();
            Builder.Register(context => riderFactory.CreateRider(context.ResolveKeyed<IRiderRegistrationContext>(registrationKey)))
                .As<IBusInstanceSpecification>()
                .SingleInstance();
            Builder.Register(context => context.Resolve<IBusInstance>().GetRider<TRider>())
                .As<TRider>()
                .SingleInstance();
        }

        protected void ThrowIfAlreadyConfigured<TRider>()
            where TRider : IRider
        {
            ThrowIfAlreadyConfigured(nameof(SetRiderFactory));
            if (!_riderTypes.Add(typeof(TRider)))
                throw new ConfigurationException($"'{typeof(TRider).Name}' can be added only once.");
        }
    }
}
