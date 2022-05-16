using Amazon;
using Amazon.Lambda.Core;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using System;
using System.Text;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Lambda2;

public class Function
{
    static readonly string senderAddress = "vakul.18@gmail.com";
    static readonly string receiverAddress = "vakul.18@gmail.com";
    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="input"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string> FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.Log($"Object : {input}");
        string? qurl = Environment.GetEnvironmentVariable("qurl");
        var receiveMsgReq = new ReceiveMessageRequest(qurl);
        receiveMsgReq.MaxNumberOfMessages = 3;
        var sqsClient = new AmazonSQSClient();
        var msgRes = await sqsClient.ReceiveMessageAsync(receiveMsgReq);
        context.Logger.Log($"MEssages received count : {msgRes.Messages.Count}");
        StringBuilder sb = new StringBuilder();
        foreach (var msg in msgRes.Messages)
        {
            sb.Append("Message :\n");
            sb.Append(msg.Body);
            context.Logger.Log($"start delete msg");
            await sqsClient.DeleteMessageAsync(qurl, msgRes.Messages[0].ReceiptHandle);
            context.Logger.Log($"DELETE done");
        }

        string htmlBody = @$"<html>
<head></head>
<body>
 
  <p>{sb.ToString()}</p>
</body>
</html>";

        using var client = new AmazonSimpleEmailServiceClient(RegionEndpoint.USEast1);
        var sendRequest = new SendEmailRequest
        {
            Source = senderAddress,
            Destination = new Destination
            {
                ToAddresses =
        new List<string> { receiverAddress }
            },
            Message = new Amazon.SimpleEmail.Model.Message
            {
                Subject = new Content("test mail"),
                Body = new Body
                {
                    Html = new Content
                    {
                        Charset = "UTF-8",
                        Data = htmlBody
                    }
                }
            },
            // If you are not using a configuration set, comment
            // or remove the following line 
            // ConfigurationSetName = configSet
        };

        try
        {
            context.Logger.Log("Sending email using Amazon SES...");
            var response = await client.SendEmailAsync(sendRequest);
            context.Logger.Log("The email was sent successfully.");
        }
        catch (Exception ex)
        {
            context.Logger.Log("The email was not sent.");
            context.Logger.Log("Error message: " + ex.Message);

        }
        return "";

    }
}
