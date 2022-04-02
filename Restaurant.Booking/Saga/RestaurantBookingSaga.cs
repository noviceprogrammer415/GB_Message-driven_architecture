﻿using MassTransit;
using Restaurant.Booking.Consumers;
using Restaurant.Messages.Implementation;
using Restaurant.Messages.Interfaces;

namespace Restaurant.Booking.Saga
{
    public class RestaurantBookingSaga : MassTransitStateMachine<RestaurantBooking>
    {
        #region constructor
        public RestaurantBookingSaga()
        {
            InstanceState(prop => prop.CurrentState);

            Event(() => BookingRequested,
                cfg => cfg.CorrelateById(context => context.Message.OrderId)
                    .SelectId(context => context.Message.OrderId));

            Event(() => TableBooked,
                cfg => cfg.CorrelateById(context => context.Message.OrderId));

            Event(() => KitchenReady,
                cfg => cfg.CorrelateById(context => context.Message.OrderId));

            Event(() => KitchenAccident,
                cfg => cfg.CorrelateById(context => context.Message.OrderId));

            CompositeEvent(() => BookingApproved,
            tracking => tracking.ReadyEventStatus, KitchenReady, TableBooked);

            Event(() => BookingRequestFault,
                cfg => cfg.CorrelateById(prop => prop.Message.Message.OrderId));

            Schedule(() => BookingExpired,
                token => token.ExpirationId, cfg =>
                {
                    cfg.Delay = TimeSpan.FromSeconds(5);
                    cfg.Received = e => e.CorrelateById(context => context.Message.OrderId);
                });

            Initially(
                When(BookingRequested)
                    .Then(action =>
                    {
                        action.Saga.CorrelationId = action.Message.OrderId;
                        action.Saga.OrderId = action.Message.OrderId;
                        action.Saga.ClientId = action.Message.ClientId;
                        Console.WriteLine($"Saga: {DateTime.Now:HH:mm:ss}");
                    })
                    //.Schedule(BookingExpired,
                    //    factory => new BookingExpire(factory.Saga),
                    //    provider => TimeSpan.FromSeconds(100))
                    .TransitionTo(AwaitingBookingApproved)
            );

            During(AwaitingBookingApproved,
                When(BookingApproved)
                    //.Unschedule(BookingExpired)
                    .Publish(factory => (INotify)new Notify(factory.Saga.ClientId,
                        factory.Saga.OrderId, "Стол успешно забронирован"))
                    .Finalize(),

                When(BookingRequestFault)
                    .Then(action => Console.WriteLine("Что-то пошло не так!"))
                    .Publish(factory => (INotify)new Notify(factory.Saga.ClientId,
                        factory.Saga.OrderId,
                        "Приносим извенения, стол забронировать не получилось."))
                    .Finalize(),

                When(BookingExpired!.Received)
                    .Then(action => Console.WriteLine($"Отмена заказа {action.Saga.OrderId}"))
                    .Finalize(),

                When(KitchenAccident)
                    .Publish(factory => (INotify)new Notify(factory.Saga.ClientId,
                        factory.Saga.OrderId,
                        $"Отмена бронирования стола по заказу в связи с отсутсвием блюда!"))
                    .Finalize());

            SetCompletedWhenFinalized();
        }
        #endregion

        #region properties
        public MassTransit.State AwaitingBookingApproved { get; private set; }
        public Event BookingApproved { get; private set; }
        public Schedule<RestaurantBooking, IBookingExpire> BookingExpired { get; private set; }
        public Event<IBookingRequest> BookingRequested { get; private set; }
        public Event<Fault<IBookingRequest>> BookingRequestFault { get; private set; }
        public Event<IKitchenReady> KitchenReady { get; private set; }
        public Event<ITableBooked> TableBooked { get; private set; }
        public Event<IKitchenAccident> KitchenAccident { get; set; }
        #endregion
    }
}
