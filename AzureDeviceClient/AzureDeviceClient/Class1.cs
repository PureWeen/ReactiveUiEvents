using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Splat;
using System.Reactive.Linq;
using System.Reactive;
using System.Linq;
using System.Reactive.Disposables;

namespace AzureIoTSuiteRemoteMonitoringHelper
{
    #region Data Contracts for serializing data
    [DataContract]
    public class DeviceProperties : IEnableLogger
    {
        [DataMember]
        internal string DeviceID;

        [DataMember]
        internal string UserId;

        [DataMember(EmitDefaultValue = false)]
        internal string CreatedTime;

        [DataMember(EmitDefaultValue = false)]
        internal string UpdatedTime;

        [DataMember(EmitDefaultValue = false)]
        internal string ModelNumber;

        [DataMember(EmitDefaultValue = false)]
        internal string SerialNumber;

        [DataMember(EmitDefaultValue = false)]
        internal string Platform;
    }

    [DataContract]
    public class CommandParameter
    {
        [DataMember]
        internal string Name;

        [DataMember]
        internal string Type;
    }

    [DataContract]
    public class Command
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        internal Collection<CommandParameter> Parameters = new Collection<CommandParameter>();
    }

    [DataContract]
    public class ReceivedMessage
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string MessageId { get; set; }
        [DataMember]
        public string CreatedTime { get; set; }
        [DataMember]
        public Dictionary<string, object> Parameters { get; set; }
    }

    [DataContract]
    public class DeviceModel
    {

        [DataMember]
        public object Battery { get; set; }

        [DataMember]
        public DeviceProperties DeviceProperties { get; set; } = new DeviceProperties();

        [DataMember]
        internal Collection<Command> Commands = new Collection<Command>();

        //[DataMember]
        //internal Collection<object> EventData = new Collection<object>();

        [DataMember]
        internal bool IsSimulatedDevice = false;

        [DataMember]
        internal string Version = "1.0";

        [DataMember]
        internal string ObjectType = "DeviceInfo";
    }
    #endregion


    /// <summary>
    /// ReceivedMessageEventArgs class
    /// Class to pass event arguments for new message received from the IoT Suite dashboard
    /// </summary>
    public class ReceivedMessageEventArgs : System.EventArgs
    {
        public ReceivedMessage Message { get; set; }

        public ReceivedMessageEventArgs(ReceivedMessage message)
        {
            Message = message;
        }
    }

    /// <summary>
    /// RemoteMonitoringDevice class
    /// Provides helper functions for easily connecting a device to Azure IoT Suite Remote Monitoring 
    /// </summary>
    public class RemoteMonitoringDevice : IEnableLogger
    {
        // Azure IoT Hub client
        private Microsoft.Azure.Devices.Client.DeviceClient deviceClient;

        // Device Model values
        public DeviceModel Model { get; set; } = new DeviceModel();


        static Lazy<RemoteMonitoringDevice> _remoteMonitoring;

        static RemoteMonitoringDevice()
        {
            _remoteMonitoring = new Lazy<RemoteMonitoringDevice>(() => new RemoteMonitoringDevice());
        }

        public static RemoteMonitoringDevice Instance
        {
            get
            {
                return _remoteMonitoring.Value;
            }
        }

        private RemoteMonitoringDevice()
        {
            // This is the place you can specify the metadata for your device. The below fields are not mandatory.
            Model.DeviceProperties.CreatedTime = DateTime.UtcNow.ToString();
            Model.DeviceProperties.UpdatedTime = DateTime.UtcNow.ToString(); 
        }


        // Collection of commands
        public Dictionary<string, object> Commands { get; set; } = new Dictionary<string, object>();
        public void AddCommand(Command CommandItem)
        {
            if (!Commands.ContainsKey(CommandItem.Name))
            {
                Commands.Add(CommandItem.Name, CommandItem);
                Model.Commands.Add(CommandItem);
            }
        }


        public bool IsConnected { get; set; } = false;

        // Sending and receiving tasks
        CancellationTokenSource TokenSource = new CancellationTokenSource();

        // Event Handler for notifying the reception of a new message from IoT Hub
        public event EventHandler onReceivedMessage;


        // Trigger for notifying reception of new message from IoT Suite dashboard
        protected virtual void OnReceivedMessage(ReceivedMessageEventArgs e)
        {
            if (onReceivedMessage != null)
                onReceivedMessage(this, e);
        }



        /// <summary>
        /// DeSerialize message
        /// </summary>
        private JObject DeSerialize(byte[] data)
        {
            string text = Encoding.UTF8.GetString(data, 0, data.Length);
            return JObject.Parse(text);
        }

        /// <summary>
        /// Serialize message
        /// </summary>
        private byte[] Serialize(object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);

        }

        public void AddData(object data, bool safeToThrowExceptions = true)
        {
            try
            { 
                Model.DeviceProperties.UpdatedTime = DateTime.UtcNow.ToString();
                sendData(data);
            }
            catch (Exception exc)
            { 
            }
        }

        public class IoTMessage
        {
            public string Key { get; set; }
            public JObject Message { get; set; }
        }



        /// <summary>
        /// Send device's telemetry data to Azure IoT Hub
        /// </summary>
        public void sendData(object eventData)
        {
            try
            {
                if (eventData == null) throw new ArgumentNullException("eventData cannot be null");
                if (Model == null) throw new ArgumentNullException("Model cannot be null"); 
                if (Model.DeviceProperties == null) throw new ArgumentNullException("Model.DeviceProperties cannot be null");
                deviceClient.SendEventBatchAsync(new[] { new Message(new byte[1999]) });
                if (!isIoTActivated)
                {
                    return;
                }


                JObject data = JObject.FromObject(Model);
                data.Merge(JObject.FromObject(new { EventData = eventData }));

                IoTMessage message = new IoTMessage()
                {
                    Key = Guid.NewGuid().ToString(),
                    Message = data
                };


            }
            catch (System.Exception e)
            {
                this.Log().ErrorException($"eventData: {eventData} sendData :{e.InnerException}", e);
            }
        }


        SerialDisposable ConnectedIoT { get; set; } = new SerialDisposable();


        bool isIoTActivated = false;

        /// <summary>
        /// Connect
        /// Connect to Azure IoT Hub ans start the send and receive loops
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connect()
        {
            try
            {
                isIoTActivated = true;

                deviceClient =
                    Microsoft.Azure.Devices.Client.DeviceClient.Create(
                        "",
                    new DeviceAuthenticationWithRegistrySymmetricKey("", ""));

                await deviceClient.OpenAsync();




            }
            catch (Exception e)
            {
                Debug.WriteLine("Error while trying to connect to IoT Hub:" + e.Message.ToString());
                deviceClient = null;
                return false;
            }
            return true;
        }


        /// <summary>
        /// Disconnect
        /// Disconnect from IoT Hub
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            if (deviceClient != null)
            {
                try
                {
                    deviceClient.CloseAsync();
                    deviceClient = null;
                    IsConnected = false;
                    ConnectedIoT.Disposable = Disposable.Empty;
                    isIoTActivated = false;
                }
                catch (Exception exc)
                {
                    this.Log().ErrorException("Error while trying close the IoT Hub connection", exc);
                    return false;
                }
            }
            return true;
        }



        /*
		public void StartReceiveListener()
		{
			Task.Factory.StartNew(async () =>
				{
					while (true)
					{
						// Receive message from Cloud (for now this is a pull because only HTTP is available for UWP applications)
						Message message = await deviceClient.ReceiveAsync();
						if (message != null)
						{
							try
							{
								// Read message and deserialize
								var obj = DeSerialize(message.GetBytes());

								ReceivedMessage command = new ReceivedMessage();
								command.Name = obj["Name"].ToString();
								command.MessageId = obj["MessageId"].ToString();
								command.CreatedTime = obj["CreatedTime"].ToString();
								command.Parameters = new Dictionary<string, object>();
								foreach (dynamic param in obj["Parameters"])
								{
									command.Parameters.Add(param.Name, param.Value);
								}

								// Invoke message received callback
								OnReceivedMessage(new ReceivedMessageEventArgs(command));

								// We received the message, indicate IoTHub we treated it
								await deviceClient.CompleteAsync(message);
							}
							catch (Exception e)
							{
								// Something went wrong. Indicate the backend that we coudn't accept the message
								Debug.WriteLine("Error while deserializing message received: " + e.Message);
								await deviceClient.RejectAsync(message);
							}
						}
						if (ct.IsCancellationRequested)
						{
							// Cancel was called
							Debug.WriteLine("Receiving task canceled");
							break;
						}
					}
				}, ct);
		}*/

    }
}
