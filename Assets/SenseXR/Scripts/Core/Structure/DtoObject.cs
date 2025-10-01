using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace SenseXR.Core.Structure
{
    /// <summary>
    /// Class that represents all Interaction and Session
    /// events collected during a simulation for a given user
    /// </summary>
    [Serializable]
    public class DtoObject
    {
        private DtoEvents _events;
        public DtoEvents Events
        {
            get
            {
                _events ??= new DtoEvents();
                return _events;
            }
            set => _events = value;
        }


        public void Add(InteractionEvent ie)
        {
            Events.InteractionEventList.Add(ie);
        }

        public void Add(SessionEvent se)
        {
            Events.SessionEventList.Add(se);
        }
    }

    [Serializable]
    public class DtoEvents
    {
        public List<InteractionEvent> InteractionEventList { get; set; } =
            new List<InteractionEvent>();

        public List<SessionEvent> SessionEventList { get; set; } = 
            new List<SessionEvent>();
    }
    

    #region Events

    /// <summary>
    /// Base class for all events that can be collected
    /// </summary>
    [Serializable]
    public class BaseEvent
    {
        public string SimulationInstanceId { get; set; }
        public string ScenarioInstanceId { get; set; }
        public EventInfoItem EventInfoItem { get; set; }
        public ActorItem ActorItem { get; set; }
        public ObjectItem ObjectItem{ get; set; }
        public TimeItem TimeItem{ get; set; }
        public List<MetadataContextItem> MetadataContextItemList { get; set; } = null;
        public ResultItem ResultItem{ get; set; }
    }
    
    /// <summary>
    /// Session-level Event class
    /// </summary>
    [Serializable]
    public class SessionEvent : BaseEvent
    {
        
    }
    
    /// <summary>
    /// Interaction-Level Event class
    /// </summary>
    [Serializable]
    public class InteractionEvent : BaseEvent
    { 
               
    }

    #endregion

    #region EventItems

    /// <summary>
    /// Name, Verb and Category of the given Event
    /// </summary>
    [Serializable]
    public class EventInfoItem
    {
        public EventCategory EventCategory { get; set; }
        public EventName EventName { get; set; }
        public EventVerb EventVerb { get; set; }
    }

    /// <summary>
    /// Who / What pertains to a certain action.
    /// i.e.: "Who did it?", "This event affect who?"
    /// </summary>
    [Serializable]
    public class ActorItem
    {
        /// <summary>
        ///     The UserId of the User, If the User is an NPC then the ID of that NPC
        /// </summary>
        public string ActorId { get; set; }
        /// <summary>
        ///     Reference name of the individual
        /// </summary>
        public ActorType ActorType { get; set; } = ActorType.User;
        /// <summary>
        ///     NPC, User, Object
        /// </summary>
        public string ActorName { get; set; }
    }

    /// <summary>
    /// Description of an object associated with the event.
    /// i.e.: Gun, Tool.
    /// </summary>
    [Serializable]
    public class ObjectItem
    {
        public string ObjectId { get; set; }
        public string ObjectName { get; set; }
        public ObjectType ObjectType { get; set; } = ObjectType.Interactable;
    }
    
    /// <summary>
    /// Outcome of the event. How was it completed?
    /// </summary>
    [Serializable]
    public class ResultItem
    {
        public ResultType ResultType { get; set; } = ResultType.Succeeded;
        public ResultMessage ResultMessage { get; set; }
        public ResultValue ResultValue { get; set; }
    }

    /// <summary>
    /// Description of the temporal location of the time
    /// </summary>
    [Serializable]
    public class TimeItem
    {
        /// <summary>
        ///     Date time of the event start time
        /// </summary>
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
        /// <summary>
        ///     Event Duration: 0 for instant
        /// </summary>
        public int EventDurationMs { get; set; } = 0;

        /// <summary>
        ///     Total Elapsed game time in milliseconds from the start of the INIT event
        /// </summary>
        public int TotalElapsedGametimeMs { get; set; } = (int)(Time.time * 1000); // Converting seconds to ms

        /// <summary>
        ///     FrameId (Count) of when that action happened,
        /// </summary>
        public int FrameId { get; set; } = Time.frameCount;
    }
    
    /// <summary>
    /// TBD
    /// </summary>
    [Serializable]
    public class MetadataContextItem
    {
        public string AttributeDescription { get; set; }
        public string AttributeName { get; set; }
    }

    #endregion

    #region EventPropertyTypes

    /// <summary>
    /// In order to allow for some extensibility, this class allows for conversion from and to string.
    /// The idea is that any project that needs to have a stricter range of EventPropertyTypes can specify
    /// and use them, building upon the ones defined here.
    /// </summary>
    [JsonConverter(typeof(EventPropertyTypeConverter))]
    public class EventPropertyType
    {
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        internal string Value { get; set; } = null;

        protected EventPropertyType(string val)
        {
            Value = val;
        }
        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(EventPropertyType ept) => ept.Value;
        public static implicit operator EventPropertyType(string s) => new EventPropertyType(s);
    }

    public class EventPropertyTypeConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((EventPropertyType)value).Value);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string);
        }
    }


    public class ObjectType : EventPropertyType
    {
        public ObjectType(string val) : base(val) { }
        public static implicit operator string(ObjectType ept) => ept.Value;
        public static implicit operator ObjectType(string s) => new ObjectType(s);
        public static ObjectType Interactable => new ObjectType("Interactable");
    }
    

    public class ActorType : EventPropertyType
    {
        public ActorType(string val) : base(val) { }
        public static implicit operator string(ActorType ept) => ept.Value;
        public static implicit operator ActorType(string s) => new ActorType(s);
        public static ActorType User => new ActorType("User");
        public static ActorType Npc => new ActorType("NPC");
    }

    public class ResultType : EventPropertyType
    {
        public ResultType(string val) : base(val) {}
        public static implicit operator string(ResultType ept) => ept.Value;
        public static implicit operator ResultType(string s) => new ResultType(s);
        public static ResultType Completed => new ResultType("COMPLETED");
        public static ResultType Succeeded => new ResultType("SUCCEEDED");
        public static ResultType Failed => new ResultType("FAILED");
        public static ResultType Interrupted => new ResultType("INTERRUPTED");
        public static ResultType Canceled => new ResultType("CANCELED");
    }
    
    public class ResultMessage : EventPropertyType
    {
        public ResultMessage(string val) : base(val) {}
        public static implicit operator string(ResultMessage ept) => ept.Value;
        public static implicit operator ResultMessage(string s) => new ResultMessage(s);
    }
    
    public class ResultValue : EventPropertyType
    {
        public ResultValue(string val) : base(val) {}
        public static implicit operator string(ResultValue ept) => ept.Value;
        public static implicit operator ResultValue(string s) => new ResultValue(s);
    }
    
    public class EventCategory : EventPropertyType
    {
        public EventCategory(string val) : base(val) {}
        public static implicit operator string(EventCategory ept) => ept.Value;
        public static implicit operator EventCategory(string s) => new EventCategory(s);

        public static EventCategory SessionEvent => new EventCategory("SESSION_EVENT");
        public static EventCategory InteractionEvent => new EventCategory("INTERACTION_EVENT");
    }

    public class EventVerb : EventPropertyType
    {
        public EventVerb(string val) : base(val) { }
        public static implicit operator string(EventVerb ept) => ept.Value;
        public static implicit operator EventVerb(string s) => new EventVerb(s);

        public static EventVerb Started => new EventVerb("STARTED");
        public static EventVerb Completed => new EventVerb("COMPLETED");
        public static EventVerb Finished => new EventVerb("FINISHED");
    }
    
    public class EventName : EventPropertyType
    {
        public EventName(string val) : base(val) {}
        public static implicit operator string(EventName ept) => ept.Value;
        public static implicit operator EventName(string s) => new EventName(s);

        public static EventName Start => new EventName("START_SESSION");
        public static EventName Leave => new EventName("LEAVE_SESSION");
    }
    
    #endregion
}
