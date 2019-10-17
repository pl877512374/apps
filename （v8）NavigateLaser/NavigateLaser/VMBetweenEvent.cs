using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;

namespace NavigateLaser
{
    public class TCPSendDataEvent : CompositePresentationEvent<TCPSendCommandName>
    {

    }

    public class TCPSendSucceedEvent : CompositePresentationEvent<TCPSendSucceedName>
    {

    }

    public class ScanDataShowEvent : CompositePresentationEvent<ScanDataContent>
    {

    }

    //生产扫描数据用
    public class ProScanDataShowEvent : CompositePresentationEvent<ScanDataContent>  
    {

    }

    //Mapping模式回复
    public class MappingDataShowEvent : CompositePresentationEvent<MappingDataContent>
    {

    }

    //BasicParam回复
    public class BasicDataShowEvent : CompositePresentationEvent<BasicDataContent>
    {

    }

    //ProductionParam回复
    public class ProductionDataShowEvent : CompositePresentationEvent<ProductionDataContent>
    {

    }

    //MotorParam回复
    public class MotorDataShowEvent : CompositePresentationEvent<MotorDataContent>
    {

    }

    //FunctionParam回复
    public class FunctionDataShowEvent : CompositePresentationEvent<FunctionDataContent>
    {

    }

    //靶标类型
    public class LandmarkTypeEvent : CompositePresentationEvent<LandmarkTypeInfo>
    {

    }

    //登录修改界面
    public class LoginChangeViewEvent : CompositePresentationEvent<LoginChangeViewName>
    {

    }

    //点线显示控制
    public class PCLDisplayTypeEvent : CompositePresentationEvent<PCLDisplayTypeContent>
    {

    }

    //mapping工作模式用
    public class MappingWorkModeEvent : CompositePresentationEvent<MappingWorkModeContent>
    {

    }

    //不同工作模式切换不同界面
    public class WorkModeViewEvent : CompositePresentationEvent<WorkModeViewContent>
    {

    }

    //接收SICK单帧
    public class ReceiveSICKEvent : CompositePresentationEvent<ReceiveSICKContent> 
    {
    
    }

    //其他VM解析完成后，由NetWindowVM更新操作完成提示
    public class OperationCompleteEvent : CompositePresentationEvent<OperationCompleteContent> 
    {
        
    }

    //靶标设置or查询回复
    public class LandmarkResponseEvent : CompositePresentationEvent<LandmarkResponseContent>
    {

    }

    //导航查询靶标回复
    public class NaviQueryEvent : CompositePresentationEvent<NaviQueryContent>
    {

    }

    //更新工作模式设置需要的位置信息
    public class UpdatePoseEvent : CompositePresentationEvent<UpdatePoseContent>
    {
        
    }

    //界面可操作权限变更
    public class ViewIsEnableEvent : CompositePresentationEvent<ViewIsEnableContent>
    {

    }

    //断开网络初始化事件
    public class DisnetInitEvent : CompositePresentationEvent<DisnetInitContent>
    {

    }
    //改变距离探测、靶标探测、获取、导航按钮颜色事件
    public class BtnColorChangeEvent : CompositePresentationEvent<string>
    {

    }
    //心跳状态查询事件
    public class QueryHeartStateEvent : CompositePresentationEvent<string>
    {

    }
    //开启连续获取事件
    public class OpenContinusFrame : CompositePresentationEvent<string>
    {

    }
}
