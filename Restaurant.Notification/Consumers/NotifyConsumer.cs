﻿using MassTransit;
using Restaurant.Messages.Interfaces;

namespace Restaurant.Notification.Consumers
{
    public class NotifyConsumer : IConsumer<INotify>
    {
        private readonly Notifier _notifier;

        public NotifyConsumer(Notifier notifier)
        {
            _notifier = notifier;
        }

        public Task Consume(ConsumeContext<INotify> context)
        {
            _notifier.Notify(context.Message.OrderId, context.Message.ClientId, context.Message.Message);

            return context.ConsumeCompleted;
        }
    }
}
