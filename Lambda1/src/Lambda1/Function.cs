using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.SQS;
using Amazon.SQS.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Lambda1;

public class Function
{
    IAmazonS3 S3Client { get; set; }

    

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        S3Client = new AmazonS3Client();
    }

    /// <summary>
    /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
    /// </summary>
    /// <param name="s3Client"></param>
    public Function(IAmazonS3 s3Client)
    {
        this.S3Client = s3Client;
    }
    
    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
    /// to respond to S3 notifications.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task<string?> FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        var s3Event = evnt.Records?[0].S3;
        if(s3Event == null)
        {
            return null;
        }

        try
        {
            StringBuilder sb = new StringBuilder();
            var response = await this.S3Client.GetObjectMetadataAsync(s3Event.Bucket.Name, s3Event.Object.Key);
            context.Logger.LogInformation($"Lets start");
            foreach(var key in response.Metadata.Keys)
            {
                context.Logger.LogInformation(key + " : " + response.Metadata[key]);
                sb.Append(key + " : " + response.Metadata[key] + "\n");    
            }
            sb.Append(s3Event.Bucket.Name + " " + s3Event.Object.Key);
            context.Logger.LogInformation($"Lets GO with {Environment.GetEnvironmentVariable("qurl")}");
            var sqsClient = new AmazonSQSClient();
            var req = new SendMessageRequest(Environment.GetEnvironmentVariable("qurl"),sb.ToString());
            req.MessageGroupId = "1";
            req.MessageDeduplicationId = "2" + (new Random()).Next();
            await sqsClient.SendMessageAsync(req);
            context.Logger.LogInformation($"Added message for {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}.");
            return "";
            
        }
        catch(Exception e)
        {
            context.Logger.LogInformation($"Error getting object {s3Event.Object.Key} from bucket {s3Event.Bucket.Name}. Make sure they exist and your bucket is in the same region as this function.");
            context.Logger.LogInformation(e.Message);
            context.Logger.LogInformation(e.StackTrace);
            throw;
        }
    }
}