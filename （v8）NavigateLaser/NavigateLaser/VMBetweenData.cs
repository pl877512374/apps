using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NavigateLaser
{
    public class TCPSendCommandName
    {
        public string SendCommandName { get; set; }
        public List<byte> SendContent { get; set; }
    }

    public class TCPSendSucceedName
    {
        public string SendSucceedName { get; set; }
    }

    public class ScanDataContent
    {
        public List<byte> ScanContent { get; set; }
    }

    //Mapping模式回复的靶标信息
    public class MappingDataContent
    {
        public string ReceiveCommandName { get; set; }
        public List<byte> MappingContent { get; set; }
    }

    //回复的BasicParam参数
    public class BasicDataContent
    {
        public string ReceiveCommandName { get; set; }
        public List<byte> ReceiveContent { get; set; }
    }

    //回复的ProductionParam参数
    public class ProductionDataContent
    {
        public string ReceiveCommandName { get; set; }
        public List<byte> ReceiveContent { get; set; }
    }
 
    //回复的FunctionParam参数
    public class FunctionDataContent
    {
        public string ReceiveCommandName { get; set; }
        public List<byte> ReceiveContent { get; set; }
    }

    //回复的MotorParam参数
    public class MotorDataContent
    {
        public string ReceiveCommandName { get; set; }
        public List<byte> ReceiveContent { get; set; }
    }

    //靶标类型
    public class LandmarkTypeInfo
    {
        public byte LandmarkTypeName { get; set; }
    }

    //登录情况修改界面参数
    public class LoginChangeViewName
    {
        public string ChangeViewName { get; set; }
    }

    //点线显示控制
    public class PCLDisplayTypeContent
    {
        public string PCLDisplayTypeName { get; set; }
    }

    //mapping工作模式用
    public class MappingWorkModeContent
    {
        public int MappingType { get; set; }  //1：正常   2：添加
        public int LayerID { get; set; }  //当前层信息
        public int WaitTime { get; set; }  //等待时间
    }

    //不同工作模式切换不同界面
    public class WorkModeViewContent
    {
        public string WorkModeViewName { get; set; }
    }

    //接收SICK单帧
    public class ReceiveSICKContent
    {
        public List<string> ReceiveContent { get; set; }
    }

    //其他VM解析完成后，由NetWindowVM更新操作完成提示
    public class OperationCompleteContent
    {
        public string NoticeContent { get; set; }
    }

    //靶标设置or查询回复
    public class LandmarkResponseContent
    {
        public string LandmarkResponseName { get; set; }
        public List<byte> LandmarkResponseData { get; set; }
    }

    //导航查询靶标回复
    public class NaviQueryContent
    {
        public string NaviQueryName { get; set; }
        public int LayerID { get; set; }  //当前层信息
    }

    //更新工作模式设置需要的位置信息
    public class UpdatePoseContent
    {
        public List<int>  PoseInfo{ get; set; }
    }

    //界面可操作权限变更
    public class ViewIsEnableContent
    {
        public bool IsEnable { get; set; }
    }

    //断开网络初始化事件
    public class DisnetInitContent
    {

    }
    //当前要改变颜色的按钮的名字
    public class BtnColorContent
    {
        public string CurrentBtnName { get; set; }
    }
}
