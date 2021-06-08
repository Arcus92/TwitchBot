namespace TwitchBot.Messages
{
    /// <summary>
    /// DS 2021-02-13: An interface for <see cref="ResponseMessage"/>.
    /// </summary>
    public interface IResponseMessage
    {
        /// <summary>
        /// Returns the text of the message
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        string GetText(OnResponseMessageParameterHandler handler = null);
    }
}
