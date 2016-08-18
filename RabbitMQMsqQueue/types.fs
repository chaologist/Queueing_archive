namespace MessagingQueueing.Queueing.RabbitMQClients
open PipelineMsgQueue.QueueFlow

type Credentials = {UserName:string;Password:string}
type QueueEnvironment ={Host:string;Credentials:Credentials option;Hosts:seq<string>;VirtualHost:string option}
type OutputRouting =  {ExchangeName:string;Routing:string}
type InputQueue ={ExchangeName:string;QueueName:string;Routings:seq<string>}
type OutputConfig = {Environment:QueueEnvironment; OutputRouting:OutputRouting} 
type ClientConfig<'a,'b> = {OutputConfig:OutputConfig; InputEnvironment:QueueEnvironment; InputQueue:InputQueue; Work:QueueWork<'a,'b>}
