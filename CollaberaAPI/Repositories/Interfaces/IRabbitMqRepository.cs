namespace CollaberaAPI
{
    /// <summary>
    /// Publish Message to RabbitMQ Queue.
    /// </summary>
    public interface IRabbitMqRepository
    {
        /// <summary>
        /// Publish Message
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        string SendMessage<T>(T message);
    }
}