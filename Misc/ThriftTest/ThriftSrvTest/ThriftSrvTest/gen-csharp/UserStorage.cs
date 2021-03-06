/**
 * Autogenerated by Thrift
 *
 * DO NOT EDIT UNLESS YOU ARE SURE THAT YOU KNOW WHAT YOU ARE DOING
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Thrift;
using Thrift.Collections;
using Thrift.Protocol;
using Thrift.Transport;
public class UserStorage {
  public interface Iface {
    void store(UserProfile user);
    UserProfile retrieve(int uid);
  }

  public class Client : Iface {
    public Client(TProtocol prot) : this(prot, prot)
    {
    }

    public Client(TProtocol iprot, TProtocol oprot)
    {
      iprot_ = iprot;
      oprot_ = oprot;
    }

    protected TProtocol iprot_;
    protected TProtocol oprot_;
    protected int seqid_;

    public TProtocol InputProtocol
    {
      get { return iprot_; }
    }
    public TProtocol OutputProtocol
    {
      get { return oprot_; }
    }


    public void store(UserProfile user)
    {
      send_store(user);
      recv_store();
    }

    public void send_store(UserProfile user)
    {
      oprot_.WriteMessageBegin(new TMessage("store", TMessageType.Call, seqid_));
      store_args args = new store_args();
      args.User = user;
      args.Write(oprot_);
      oprot_.WriteMessageEnd();
      oprot_.Transport.Flush();
    }

    public void recv_store()
    {
      TMessage msg = iprot_.ReadMessageBegin();
      if (msg.Type == TMessageType.Exception) {
        TApplicationException x = TApplicationException.Read(iprot_);
        iprot_.ReadMessageEnd();
        throw x;
      }
      store_result result = new store_result();
      result.Read(iprot_);
      iprot_.ReadMessageEnd();
      return;
    }

    public UserProfile retrieve(int uid)
    {
      send_retrieve(uid);
      return recv_retrieve();
    }

    public void send_retrieve(int uid)
    {
      oprot_.WriteMessageBegin(new TMessage("retrieve", TMessageType.Call, seqid_));
      retrieve_args args = new retrieve_args();
      args.Uid = uid;
      args.Write(oprot_);
      oprot_.WriteMessageEnd();
      oprot_.Transport.Flush();
    }

    public UserProfile recv_retrieve()
    {
      TMessage msg = iprot_.ReadMessageBegin();
      if (msg.Type == TMessageType.Exception) {
        TApplicationException x = TApplicationException.Read(iprot_);
        iprot_.ReadMessageEnd();
        throw x;
      }
      retrieve_result result = new retrieve_result();
      result.Read(iprot_);
      iprot_.ReadMessageEnd();
      if (result.__isset.success) {
        return result.Success;
      }
      throw new TApplicationException(TApplicationException.ExceptionType.MissingResult, "retrieve failed: unknown result");
    }

  }
  public class Processor : TProcessor {
    public Processor(Iface iface)
    {
      iface_ = iface;
      processMap_["store"] = store_Process;
      processMap_["retrieve"] = retrieve_Process;
    }

    protected delegate void ProcessFunction(int seqid, TProtocol iprot, TProtocol oprot);
    private Iface iface_;
    protected Dictionary<string, ProcessFunction> processMap_ = new Dictionary<string, ProcessFunction>();

    public bool Process(TProtocol iprot, TProtocol oprot)
    {
      try
      {
        TMessage msg = iprot.ReadMessageBegin();
        ProcessFunction fn;
        processMap_.TryGetValue(msg.Name, out fn);
        if (fn == null) {
          TProtocolUtil.Skip(iprot, TType.Struct);
          iprot.ReadMessageEnd();
          TApplicationException x = new TApplicationException (TApplicationException.ExceptionType.UnknownMethod, "Invalid method name: '" + msg.Name + "'");
          oprot.WriteMessageBegin(new TMessage(msg.Name, TMessageType.Exception, msg.SeqID));
          x.Write(oprot);
          oprot.WriteMessageEnd();
          oprot.Transport.Flush();
          return true;
        }
        fn(msg.SeqID, iprot, oprot);
      }
      catch (IOException)
      {
        return false;
      }
      return true;
    }

    public void store_Process(int seqid, TProtocol iprot, TProtocol oprot)
    {
      store_args args = new store_args();
      args.Read(iprot);
      iprot.ReadMessageEnd();
      store_result result = new store_result();
      iface_.store(args.User);
      oprot.WriteMessageBegin(new TMessage("store", TMessageType.Reply, seqid)); 
      result.Write(oprot);
      oprot.WriteMessageEnd();
      oprot.Transport.Flush();
    }

    public void retrieve_Process(int seqid, TProtocol iprot, TProtocol oprot)
    {
      retrieve_args args = new retrieve_args();
      args.Read(iprot);
      iprot.ReadMessageEnd();
      retrieve_result result = new retrieve_result();
      result.Success = iface_.retrieve(args.Uid);
      oprot.WriteMessageBegin(new TMessage("retrieve", TMessageType.Reply, seqid)); 
      result.Write(oprot);
      oprot.WriteMessageEnd();
      oprot.Transport.Flush();
    }

  }


  [Serializable]
  public partial class store_args : TBase
  {
    private UserProfile _user;

    public UserProfile User
    {
      get
      {
        return _user;
      }
      set
      {
        __isset.user = true;
        this._user = value;
      }
    }


    public Isset __isset;
    [Serializable]
    public struct Isset {
      public bool user;
    }

    public store_args() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.Struct) {
              User = new UserProfile();
              User.Read(iprot);
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("store_args");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (User != null && __isset.user) {
        field.Name = "user";
        field.Type = TType.Struct;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        User.Write(oprot);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("store_args(");
      sb.Append("User: ");
      sb.Append(User== null ? "<null>" : User.ToString());
      sb.Append(")");
      return sb.ToString();
    }

  }


  [Serializable]
  public partial class store_result : TBase
  {

    public store_result() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("store_result");
      oprot.WriteStructBegin(struc);

      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("store_result(");
      sb.Append(")");
      return sb.ToString();
    }

  }


  [Serializable]
  public partial class retrieve_args : TBase
  {
    private int _uid;

    public int Uid
    {
      get
      {
        return _uid;
      }
      set
      {
        __isset.uid = true;
        this._uid = value;
      }
    }


    public Isset __isset;
    [Serializable]
    public struct Isset {
      public bool uid;
    }

    public retrieve_args() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 1:
            if (field.Type == TType.I32) {
              Uid = iprot.ReadI32();
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("retrieve_args");
      oprot.WriteStructBegin(struc);
      TField field = new TField();
      if (__isset.uid) {
        field.Name = "uid";
        field.Type = TType.I32;
        field.ID = 1;
        oprot.WriteFieldBegin(field);
        oprot.WriteI32(Uid);
        oprot.WriteFieldEnd();
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("retrieve_args(");
      sb.Append("Uid: ");
      sb.Append(Uid);
      sb.Append(")");
      return sb.ToString();
    }

  }


  [Serializable]
  public partial class retrieve_result : TBase
  {
    private UserProfile _success;

    public UserProfile Success
    {
      get
      {
        return _success;
      }
      set
      {
        __isset.success = true;
        this._success = value;
      }
    }


    public Isset __isset;
    [Serializable]
    public struct Isset {
      public bool success;
    }

    public retrieve_result() {
    }

    public void Read (TProtocol iprot)
    {
      TField field;
      iprot.ReadStructBegin();
      while (true)
      {
        field = iprot.ReadFieldBegin();
        if (field.Type == TType.Stop) { 
          break;
        }
        switch (field.ID)
        {
          case 0:
            if (field.Type == TType.Struct) {
              Success = new UserProfile();
              Success.Read(iprot);
            } else { 
              TProtocolUtil.Skip(iprot, field.Type);
            }
            break;
          default: 
            TProtocolUtil.Skip(iprot, field.Type);
            break;
        }
        iprot.ReadFieldEnd();
      }
      iprot.ReadStructEnd();
    }

    public void Write(TProtocol oprot) {
      TStruct struc = new TStruct("retrieve_result");
      oprot.WriteStructBegin(struc);
      TField field = new TField();

      if (this.__isset.success) {
        if (Success != null) {
          field.Name = "Success";
          field.Type = TType.Struct;
          field.ID = 0;
          oprot.WriteFieldBegin(field);
          Success.Write(oprot);
          oprot.WriteFieldEnd();
        }
      }
      oprot.WriteFieldStop();
      oprot.WriteStructEnd();
    }

    public override string ToString() {
      StringBuilder sb = new StringBuilder("retrieve_result(");
      sb.Append("Success: ");
      sb.Append(Success== null ? "<null>" : Success.ToString());
      sb.Append(")");
      return sb.ToString();
    }

  }

}
