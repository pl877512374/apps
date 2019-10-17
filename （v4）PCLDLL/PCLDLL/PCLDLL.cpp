/* \author Geoffrey Biggs */

#include <iostream>
#include "PCLDLL.h"
#include <boost/thread/thread.hpp>
#include <pcl/common/common_headers.h>
#include <pcl/common/transforms.h>
#include <pcl/features/normal_3d.h>
#include <pcl/io/pcd_io.h>
#include <pcl/visualization/pcl_visualizer.h>
#include <pcl/console/parse.h>
#include "math.h"
#include "vtkCylinderSource.h"
#include "vtkPolyDataMapper.h"
#include "vtkActor.h"
#include "vtkRenderer.h"
#include "vtkRenderWindow.h"
#include "vtkRenderWindowInteractor.h"
#include "vtkProperty.h"
#include "vtkCamera.h"
#include "vtkAxisActor2D.h"


#include "vtkLegendScaleActor.h"
#include "vtkPointPicker.h"
#include "vtkPicker.h"
#include "vtkActor.h"
#include "vtkAssemblyNode.h"
#include "vtkAssemblyPath.h"
#include "vtkCamera.h"
#include "vtkCommand.h"
#include "vtkImageData.h"
#include "vtkLODProp3D.h"
#include "vtkMapper.h"
#include "vtkMath.h"
#include "vtkObjectFactory.h"
#include "vtkPoints.h"
#include "vtkProp3DCollection.h"
#include "vtkProperty.h"
#include "vtkRenderWindow.h"
#include "vtkRenderer.h"
#include "vtkTransform.h"
#include "vtkVertex.h"
#include "vtkVolume.h"
#include "vtkAbstractVolumeMapper.h"
#include "vtkBox.h"
#include "vtkImageActor.h"
#include "vtkAbstractPropPicker.h"
#include <pcl/common/transforms.h>
#define PI  3.14159265358979323846
#define LMColorArrayCount 12000//靶标颜色显示数组长度
typedef pcl::PointXYZRGBA PointT;
typedef pcl::PointCloud<PointT> PointCloudT;
boost::shared_ptr<pcl::visualization::PCLVisualizer> viewer;
vtkSmartPointer<vtkRenderWindow> win;
vtkSmartPointer<vtkRenderWindowInteractor> iren;
vtkSmartPointer<vtkLegendScaleActor> grid_actor_;
pcl::PointCloud<pcl::PointXYZRGB>::Ptr Laser_cloud_ptr(new pcl::PointCloud<pcl::PointXYZRGB>);
pcl::PointCloud<pcl::PointXYZ>::Ptr LandMark_cloud_ptr(new pcl::PointCloud<pcl::PointXYZ>);       //实时获取的靶标、填充显示
pcl::PointCloud<pcl::PointXYZ>::Ptr SetLandMark_cloud_ptr(new pcl::PointCloud<pcl::PointXYZ>);    //设置靶标、镂空显示
vector<int> SetLandMark_ID;   //记录设置的靶标的ID
map<int, pcl::PointXYZ> mapScanLMXY;//记录扫描的时候扫到的靶标的ID,X,Y数据
int CurrentRedSetLMID = -1;
int CurrentRedLMNO = -1;
PCLDLL *pcldll = NULL;
int lastLMType = 0;   //靶标显示类型
int EveryLMFlag[40];
double LastLaserPose[4] = { -1, 0, 0, 0 }; //上一次自身位置信息 

boost::mutex m_ViewMutex;
bool FlagPointDisplay = true;  //true是点  false是线

typedef struct _ChangeShapesInfo
{
	bool ChangeShapesFlg;    //0不需要修改，1需要修改
	unsigned int ChangeSizeType;    //0大尺寸，1中尺寸，2小尺寸
}ChangeShapesInfo;
ChangeShapesInfo g_ChangeShapesInfo;

//回调
typedef void(*CallBackFun)(int x, int y);
bool IsSetLMClicked = FALSE, IsScanLMClicked = FALSE;
//显示三维坐标变量
double pval[3] = { 0 };
double Tolerance;  //tolerance for computation (% of window)
double MapperPosition[3]; //selection point in untransformed coordinates

vtkAbstractMapper3D *Mapper; //selected mapper (if the prop has a mapper)
vtkDataSet *DataSet; //selected dataset (if there is one)

double GlobalTMin; //parametric coordinate along pick ray where hit occured
vtkTransform *Transform; //use to perform ray transformation
vtkActorCollection *Actors; //candidate actors (based on bounding box)
vtkProp3DCollection *Prop3Ds; //candidate actors (based on bounding box)
vtkPoints *PickedPositions; // candidate positions

vtkRenderer* FindPokedRenderer(int x, int y)
{
	vtkRendererCollection *rc;
	vtkRenderer *aren;
	vtkRenderer *currentRenderer = NULL, *interactiveren = NULL, *viewportren = NULL;
	int numRens, i;

	rc = win->GetRenderers();
	numRens = rc->GetNumberOfItems();

	for (i = numRens - 1; (i >= 0) && !currentRenderer; i--)
	{
		aren = static_cast<vtkRenderer *>(rc->GetItemAsObject(i));
		if (aren->IsInViewport(x, y) && aren->GetInteractive())
		{
			currentRenderer = aren;
		}

		if (interactiveren == NULL && aren->GetInteractive())
		{
			// Save this renderer in case we can't find one in the viewport that
			// is interactive.
			interactiveren = aren;
		}
		if (viewportren == NULL && aren->IsInViewport(x, y))
		{
			// Save this renderer in case we can't find one in the viewport that 
			// is interactive.
			viewportren = aren;
		}
	}//for all renderers

	// We must have a value.  If we found an interactive renderer before, that's
	// better than a non-interactive renderer.
	if (currentRenderer == NULL)
	{
		currentRenderer = interactiveren;
	}

	// We must have a value.  If we found a renderer that is in the viewport,
	// that is better than any old viewport (but not as good as an interactive
	// one).
	if (currentRenderer == NULL)
	{
		currentRenderer = viewportren;
	}

	// We must have a value - take anything.
	if (currentRenderer == NULL)
	{
		aren = rc->GetFirstRenderer();
		currentRenderer = aren;
	}

	return currentRenderer;
}

void PCLDLL::MarkPicked(vtkAssemblyPath *path, vtkProp3D *prop3D, vtkAbstractMapper3D *m,
	double tMin, double mapperPos[3])
{
	int i;
	vtkMapper *mapper;
	vtkAbstractVolumeMapper *volumeMapper;
	vtkImageActor *imageActor;

	SetPath(path);
	GlobalTMin = tMin;

	for (i = 0; i < 3; i++)
	{
		MapperPosition[i] = mapperPos[i];
	}
	if ((mapper = vtkMapper::SafeDownCast(m)) != NULL)
	{
		DataSet = mapper->GetInput();
		Mapper = mapper;
	}
	else if ((volumeMapper = vtkAbstractVolumeMapper::SafeDownCast(m)) != NULL)
	{
		DataSet = volumeMapper->GetDataSetInput();
		Mapper = volumeMapper;
	}
	else if ((imageActor = vtkImageActor::SafeDownCast(prop3D)) != NULL)
	{
		DataSet = imageActor->GetInput();
		Mapper = NULL;
	}
	else
	{
		DataSet = NULL;
	}

	// The point has to be transformed back into world coordinates.
	// Note: it is assumed that the transform is in the correct state.
	Transform->TransformPoint(mapperPos, PickPosition);
}

double PCLDLL::IntersectWithLine(double p1[3], double p2[3],
	double vtkNotUsed(tol),
	vtkAssemblyPath *path,
	vtkProp3D *prop3D,
	vtkAbstractMapper3D *mapper)
{
	int i;
	double center[3], t, ray[3], rayFactor;
	vtkImageActor *imageActor = NULL;

	// Get the data from the modeler
	if (mapper != NULL)
	{
		mapper->GetCenter(center);
	}
	else if ((imageActor = vtkImageActor::SafeDownCast(prop3D)) != NULL)
	{
		double bounds[6];
		imageActor->GetDisplayBounds(bounds);
		for (i = 0; i < 3; i++)
		{
			center[i] = 0.5*(bounds[2 * i] + bounds[2 * i + 1]);
		}
	}
	else
	{
		return VTK_DOUBLE_MAX;
	}

	for (i = 0; i < 3; i++)
	{
		ray[i] = p2[i] - p1[i];
	}
	if ((rayFactor = vtkMath::Dot(ray, ray)) == 0.0)
	{
		return 2.0;
	}

	// Project the center point onto the ray and determine its parametric value
	//
	t = (ray[0] * (center[0] - p1[0]) + ray[1] * (center[1] - p1[1])
		+ ray[2] * (center[2] - p1[2])) / rayFactor;

	if (t >= 0.0 && t <= 1.0 && t < GlobalTMin)
	{
		MarkPicked(path, prop3D, mapper, t, center);
	}
	return t;
}

// Initialize the picking process.
void PCLDLL::SelfInitialize()
{
	vtkAbstractPropPicker::Initialize();
	Actors->RemoveAllItems();
	Prop3Ds->RemoveAllItems();
	PickedPositions->Reset();

	MapperPosition[0] = 0.0;
	MapperPosition[1] = 0.0;
	MapperPosition[2] = 0.0;

	Mapper = NULL;
	DataSet = NULL;
	GlobalTMin = VTK_DOUBLE_MAX;
}

int PCLDLL::Pick(double selectionX, double selectionY, double selectionZ, vtkRenderer *renderer)
{
	int i;
	vtkProp *prop;
	vtkCamera *camera;
	vtkAbstractMapper3D *mapper = NULL;
	double p1World[4], p2World[4], p1Mapper[4], p2Mapper[4];
	int winSize[2] = { 1, 1 };
	double x, y, t;
	double *viewport;
	double cameraPos[4], cameraFP[4];
	double *displayCoords, *worldCoords;
	double *clipRange;
	double ray[3], rayLength;
	int pickable;
	int LODId;
	double windowLowerLeft[4], windowUpperRight[4];
	double bounds[6], tol;
	double tF, tB;
	double hitPosition[3];
	double cameraDOP[3];
	double PickPosition[3];

	bounds[0] = bounds[1] = bounds[2] = bounds[3] = bounds[4] = bounds[5] = 0;
	double SelectionPoint[3];
	//  Initialize picking process
	SelfInitialize();
	vtkRenderer *Renderer = renderer;
	SelectionPoint[0] = selectionX;
	SelectionPoint[1] = selectionY;
	SelectionPoint[2] = selectionZ;

	// Invoke start pick method if defined
	InvokeEvent(vtkCommand::StartPickEvent, NULL);

	if (renderer == NULL)
	{
		vtkErrorMacro(<< "Must specify renderer!");
		return 0;
	}

	// Get camera focal point and position. Convert to display (screen)
	// coordinates. We need a depth value for z-buffer.
	//
	camera = renderer->GetActiveCamera();
	camera->GetPosition(cameraPos);
	cameraPos[3] = 1.0;
	camera->GetFocalPoint(cameraFP);//焦点
	cameraFP[3] = 1.0;

	renderer->SetWorldPoint(cameraFP[0], cameraFP[1], cameraFP[2], cameraFP[3]);
	renderer->WorldToDisplay();
	displayCoords = renderer->GetDisplayPoint();
	selectionZ = displayCoords[2];

	// Convert the selection point into world coordinates.
	//
	renderer->SetDisplayPoint(selectionX, selectionY, selectionZ);
	renderer->DisplayToWorld();
	worldCoords = renderer->GetWorldPoint();
	if (worldCoords[3] == 0.0)
	{
		vtkErrorMacro(<< "Bad homogeneous coordinates");
		return 0;
	}
	for (i = 0; i < 3; i++)
	{
		PickPosition[i] = worldCoords[i] / worldCoords[3];
	}

	//  Compute the ray endpoints.  The ray is along the line running from
	//  the camera position to the selection point, starting where this line
	//  intersects the front clipping plane, and terminating where this
	//  line intersects the back clipping plane.
	for (i = 0; i < 3; i++)
	{
		ray[i] = PickPosition[i] - cameraPos[i];
	}
	for (i = 0; i < 3; i++)
	{
		cameraDOP[i] = cameraFP[i] - cameraPos[i];
	}

	vtkMath::Normalize(cameraDOP);

	if ((rayLength = vtkMath::Dot(cameraDOP, ray)) == 0.0)
	{
		//vtkWarningMacro("Cannot process points");
		return 0;
	}

	clipRange = camera->GetClippingRange();

	if (camera->GetParallelProjection())
	{
		tF = clipRange[0] - rayLength;
		tB = clipRange[1] - rayLength;
		for (i = 0; i < 3; i++)
		{
			p1World[i] = PickPosition[i] + tF*cameraDOP[i];
			p2World[i] = PickPosition[i] + tB*cameraDOP[i];
		}
	}
	else
	{
		tF = clipRange[0] / rayLength;
		tB = clipRange[1] / rayLength;
		for (i = 0; i < 3; i++)
		{
			p1World[i] = cameraPos[i] + tF*ray[i];
			p2World[i] = cameraPos[i] + tB*ray[i];
		}
	}
	p1World[3] = p2World[3] = 1.0;

	// Compute the tolerance in world coordinates.  Do this by
	// determining the world coordinates of the diagonal points of the
	// window, computing the width of the window in world coordinates, and
	// multiplying by the tolerance.
	//
	viewport = renderer->GetViewport();
	if (renderer->GetRenderWindow())
	{
		int *winSizePtr = renderer->GetRenderWindow()->GetSize();
		if (winSizePtr)
		{
			winSize[0] = winSizePtr[0];
			winSize[1] = winSizePtr[1];
		}
	}
	x = winSize[0] * viewport[0];
	y = winSize[1] * viewport[1];
	renderer->SetDisplayPoint(x, y, selectionZ);
	renderer->DisplayToWorld();
	renderer->GetWorldPoint(windowLowerLeft);

	x = winSize[0] * viewport[2];
	y = winSize[1] * viewport[3];
	renderer->SetDisplayPoint(x, y, selectionZ);
	renderer->DisplayToWorld();
	renderer->GetWorldPoint(windowUpperRight);

	for (tol = 0.0, i = 0; i < 3; i++)
	{
		tol += (windowUpperRight[i] - windowLowerLeft[i]) *
			(windowUpperRight[i] - windowLowerLeft[i]);
	}

	tol = sqrt(tol) *Tolerance;

	//  Loop over all props.  Transform ray (defined from position of
	//  camera to selection point) into coordinates of mapper (not
	//  transformed to actors coordinates!  Reduces overall computation!!!).
	//  Note that only vtkProp3D's can be picked by vtkPicker.
	//
	vtkPropCollection *props;
	vtkProp *propCandidate;
	if (PickFromList)
	{
		props = GetPickList();
	}
	else
	{
		props = renderer->GetViewProps();
	}

	vtkActor *actor;
	vtkLODProp3D *prop3D;
	vtkVolume *volume;
	vtkImageActor *imageActor = 0;
	vtkAssemblyPath *path;
	vtkProperty *tempProperty;
	Transform->PostMultiply();
	vtkCollectionSimpleIterator pit;
	double scale[3];
	for (props->InitTraversal(pit); (prop = props->GetNextProp(pit));)
	{
		for (prop->InitPathTraversal(); (path = prop->GetNextPath());)
		{
			pickable = 0;
			actor = NULL;
			propCandidate = path->GetLastNode()->GetViewProp();
			if (propCandidate->GetPickable() && propCandidate->GetVisibility())
			{
				pickable = 1;
				if ((actor = vtkActor::SafeDownCast(propCandidate)) != NULL)
				{
					mapper = actor->GetMapper();
					if (actor->GetProperty()->GetOpacity() <= 0.0)
					{
						pickable = 0;
					}
				}
				else if ((prop3D = vtkLODProp3D::SafeDownCast(propCandidate)) != NULL)
				{
					LODId = prop3D->GetPickLODID();
					mapper = prop3D->GetLODMapper(LODId);

					// if the mapper is a vtkMapper (as opposed to a vtkVolumeMapper),
					// then check the transparency to see if the object is pickable
					if (vtkMapper::SafeDownCast(mapper) != NULL)
					{
						prop3D->GetLODProperty(LODId, &tempProperty);
						if (tempProperty->GetOpacity() <= 0.0)
						{
							pickable = 0;
						}
					}
				}
				else if ((volume = vtkVolume::SafeDownCast(propCandidate)) != NULL)
				{
					mapper = volume->GetMapper();
				}
				else if ((imageActor = vtkImageActor::SafeDownCast(propCandidate)))
				{
					mapper = 0;
				}
				else
				{
					pickable = 0; //only vtkProp3D's (actors and volumes) can be picked
				}
			}

			//  If actor can be picked, get its composite matrix, invert it, and
			//  use the inverted matrix to transform the ray points into mapper
			//  coordinates.
			if (pickable)
			{
				vtkMatrix4x4 *lastMatrix = path->GetLastNode()->GetMatrix();
				if (lastMatrix == NULL)
				{
					vtkErrorMacro(<< "Pick: Null matrix.");
					return 0;
				}
				Transform->SetMatrix(lastMatrix);
				Transform->Push();
				Transform->Inverse();
				Transform->GetScale(scale); //need to scale the tolerance

				Transform->TransformPoint(p1World, p1Mapper);
				Transform->TransformPoint(p2World, p2Mapper);

				for (i = 0; i < 3; i++)
				{
					ray[i] = p2Mapper[i] - p1Mapper[i];
				}

				Transform->Pop();

				//  Have the ray endpoints in mapper space, now need to compare this
				//  with the mapper bounds to see whether intersection is possible.
				//
				//  Get the bounding box of the modeller.  Note that the tolerance is
				//  added to the bounding box to make sure things on the edge of the
				//  bounding box are picked correctly.
				if (mapper != NULL)
				{
					mapper->GetBounds(bounds);
				}
				else if (imageActor != NULL)
				{
					imageActor->GetDisplayBounds(bounds);
				}

				bounds[0] -= tol; bounds[1] += tol;
				bounds[2] -= tol; bounds[3] += tol;
				bounds[4] -= tol; bounds[5] += tol;

				if (vtkBox::IntersectBox(bounds, p1Mapper, ray, hitPosition, t))
				{
					t = IntersectWithLine(
						p1Mapper, p2Mapper, tol*0.333*(scale[0] + scale[1] + scale[2]),
						path, static_cast<vtkProp3D *>(propCandidate), mapper);

					if (t < VTK_DOUBLE_MAX)
					{
						pval[0] = (1.0 - t)*p1World[0] + t*p2World[0];
						pval[1] = (1.0 - t)*p1World[1] + t*p2World[1];
						pval[2] = (1.0 - t)*p1World[2] + t*p2World[2];

						// The IsItemPresent method returns "index+1"
						int prevIndex = Prop3Ds->IsItemPresent(prop) - 1;

						if (prevIndex >= 0)
						{
							// If already in list, set point to the closest point
							double oldp[3];
							PickedPositions->GetPoint(prevIndex, oldp);
							if (vtkMath::Distance2BetweenPoints(p1World, pval) <
								vtkMath::Distance2BetweenPoints(p1World, oldp))
							{
								PickedPositions->SetPoint(prevIndex, pval);
							}
						}
						else
						{
							Prop3Ds->AddItem(static_cast<vtkProp3D *>(prop));

							PickedPositions->InsertNextPoint(pval);

							// backwards compatibility: also add to Actors
							if (actor)
							{
								Actors->AddItem(actor);
							}
						}
					}
				}
			}//if visible and pickable and not transparent
		}//for all parts
	}//for all actors

	int picked = 0;

	if (Path)
	{
		// Invoke pick method if one defined - prop goes first
		Path->GetFirstNode()->GetViewProp()->Pick();
		InvokeEvent(vtkCommand::PickEvent, NULL);
		picked = 1;
	}

	// Invoke end pick method if defined
	InvokeEvent(vtkCommand::EndPickEvent, NULL);

	return picked;
}

/*
*	在屏幕上添加网格
*/
void SetUpGrid(int grid_size, float grid_step)
{
	string name_x = string("line_x_");
	string name_y = string("line_y_");
	string name_line_x = string("");
	string name_line_y = string("");

	string name_xScale = string("Scale_x_");
	string name_Scale_x = string("");
	string name_yScale = string("Scale_y_");
	string name_Scale_y = string("");

	viewer->removeAllShapes();

	pcl::PointXYZ p1(0, 0, 0), p2(0, 0, 0);
	pcl::PointXYZ p3(p1), p4(p2);
	pcl::PointXYZ px(p1), py(p2);
	int startpos = grid_step*grid_size / 2;
	for (int j = 0; j <= grid_size; j++)
	{
		p1.x = -startpos + j*grid_step; p1.y = -startpos;
		p2.x = -startpos + j*grid_step; p2.y = startpos;
		name_line_x = name_x + to_string((_Longlong)j);
		name_Scale_x = name_xScale + to_string((_Longlong)j);
		if (j == grid_size / 2)
		{
			viewer->addLine(p1, p2, 0.25, 0.25, 0.25, name_line_x);
		}
		else
		{
			viewer->addLine(p1, p2, 0.827451, 0.827451, 0.827451, name_line_x);
			px.x = p1.x;
			viewer->addText3D(to_string((_Longlong)p1.x), px, (grid_step / 5.0), 0, 0, 0, name_Scale_x);
			if (p1.x == startpos)
			{
				px.x += 2.5;//原来为1
				viewer->addText3D("X(m)", px, (grid_step / 5.0 + 1), 0, 0, 0, "Axis_x");
			}
		}

		p3.x = -startpos; p3.y = -startpos + j*grid_step;
		p4.x = startpos;  p4.y = -startpos + j*grid_step;
		name_line_y = name_y + to_string((_Longlong)j);
		name_Scale_y = name_yScale + to_string((_Longlong)j);
		if (j == grid_size / 2)
		{
			viewer->addLine(p3, p4, 0.25, 0.25, 0.25, name_line_y);
		}
		else
		{
			viewer->addLine(p3, p4, 0.827451, 0.827451, 0.827451, name_line_y);
			py.y = p3.y;
			viewer->addText3D(to_string((_Longlong)p3.y), py, (grid_step / 5.0), 0, 0, 0, name_Scale_y);
			if (p3.y == startpos)
			{
				py.y += 2.5;//原来为1
				viewer->addText3D("Y(m)", py, (grid_step / 5.0 + 1), 0, 0, 0, "Axis_y");
			}
		}
	}
}

void initVTKCamera(void)
{
	/*相机参数*/
	viewer->camera_.clip[0] = 1.000522511;
	viewer->camera_.clip[1] = 1.522511;
	viewer->camera_.focal[0] = 0;
	viewer->camera_.focal[1] = 0;
	viewer->camera_.focal[2] = 0;
	viewer->camera_.pos[0] = 0.0;
	viewer->camera_.pos[1] = 0.0;
	viewer->camera_.pos[2] = 20 * 3 * 5;
	viewer->camera_.view[0] = 0;
	viewer->camera_.view[1] = 1;
	viewer->camera_.view[2] = 0;
	viewer->camera_.fovy = 0.78539 * 0;
	viewer->updateCamera();

	win->GetRenderers()->GetFirstRenderer()->GetActiveCamera()->SetParallelProjection(1);

	viewer->resetCamera();
}

void CreatPointCloudView(pcl::PointCloud<pcl::PointXYZRGB>::ConstPtr cloud)
{
	//创建3D窗口并添加点云    
	pcl::visualization::PointCloudColorHandlerRGBField<pcl::PointXYZRGB> rgb(cloud);
	viewer->addPointCloud<pcl::PointXYZRGB>(cloud, rgb, "sample cloud");
	viewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 3, "sample cloud");
	viewer->resetCamera();
}

void ClickPointCallBack(const pcl::visualization::PointPickingEvent& event)
{
	viewer->removePointCloud("Rightclicked_points");
	int index = event.getPointIndex();
	if (index == -1)
		return;
	uint8_t r(255), g(15), b(15);

	pcl::PointXYZRGBA current_point;
	event.getPoint(current_point.x, current_point.y, current_point.z);

	/*
	使选中的点变黑
	*/
	PointCloudT::Ptr clicked_points_3d(new PointCloudT);
	clicked_points_3d->points.push_back(current_point);
	pcl::visualization::PointCloudColorHandlerCustom<PointT> black(clicked_points_3d, 0, 0, 0);
	viewer->removePointCloud("ShiftLeftclicked_points");
	viewer->addPointCloud(clicked_points_3d, black, "ShiftLeftclicked_points");
	viewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 10, "ShiftLeftclicked_points");

	pcl::PointXYZRGB Origin_point;
	Origin_point.x = LastLaserPose[2];
	Origin_point.y = LastLaserPose[3];
	Origin_point.z = current_point.z;
	current_point.x = current_point.x - Origin_point.x;//平移
	current_point.y = current_point.y - Origin_point.y;
	double roaAngle = LastLaserPose[1] / 180 * PI;
	float tempX = current_point.x;
	current_point.x = current_point.x*cos(roaAngle) + current_point.y*sin(roaAngle);//旋转
	current_point.y = current_point.y*cos(roaAngle) - tempX*sin(roaAngle);
	int PX = int(current_point.x * 1000);
	int PY = int(current_point.y * 1000);
	int PZ = int(current_point.z * 1000);
	int PDistance = (int)(sqrt(PX*PX + PY*PY*1.0));
	/*
	输出点的信息
	*/
	std::stringstream ss;
	ss << "  No. : " << (index + 1) << "\nX(mm): " << PX << "\nY(mm): " << PY << "\nZ(mm): " << PZ << "\nD(mm): " << PDistance;
	viewer->removeShape("text", 0);
	viewer->addText(ss.str(), 1, 20, 13, 0, 0, 0, "text", 0);
}

extern "C" _declspec(dllexport) void EqualProportionProjectionByXY(double x, double y)
{
	std::vector<pcl::visualization::Camera> camera;
	viewer->getCameras(camera);
	Eigen::Vector3d position(camera[0].pos);
	double norm = position.norm();
	camera[0].view[0] = 0;
	camera[0].view[1] = 1;
	camera[0].view[2] = 0;
	camera[0].pos[0] = x;
	camera[0].pos[1] = y;
	camera[0].pos[2] = norm;
	camera[0].focal[0] = x;
	camera[0].focal[1] = y;
	camera[0].focal[2] = -1;
	viewer->camera_ = camera[0];
	viewer->updateCamera();
	win->GetRenderers()->GetFirstRenderer()->GetActiveCamera()->SetParallelProjection(1);
	win->Render();
}

void mouseEventOccurred(const pcl::visualization::MouseEvent &event)
{
	if (event.getButton() == pcl::visualization::MouseEvent::RightButton&& event.getType() == pcl::visualization::MouseEvent::MouseButtonRelease) //event.getButton() == pcl::visualization::MouseEvent::RightButton&& event.getType() == pcl::visualization::MouseEvent::MouseButtonRelease
	{
		viewer->removePointCloud("ShiftLeftclicked_points");
		IsSetLMClicked = TRUE;
		IsScanLMClicked = TRUE;
		int mouse_x = event.getX();
		int mouse_y = event.getY();
		vtkRenderer *ren = FindPokedRenderer(mouse_x, mouse_y);
		if (!pcldll)
		{
			pcldll = new PCLDLL();
		}
		pcldll->Pick(mouse_x, mouse_y, 0, ren);
		PointCloudT::Ptr clicked_points_3d(new PointCloudT);
		PointT current_point;
		current_point.x = pval[0];
		current_point.y = pval[1];
		current_point.z = pval[2];
		clicked_points_3d->points.push_back(current_point);
		pcl::visualization::PointCloudColorHandlerCustom<PointT> red(clicked_points_3d, 255, 0, 0);
		viewer->removePointCloud("Rightclicked_points");
		//viewer->addPointCloud(clicked_points_3d, red, "Rightclicked_points");
		/*	viewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 6, "Rightclicked_points");
			std::stringstream ss;
			ss << "\nX(mm): " << (int)(pval[0] * 1000) << "\nY(mm): " << (int)(pval[1] * 1000) << "\nz(mm): " << (int)(pval[2] * 1000);
			viewer->removeShape("text", 0);
			viewer->addText(ss.str(), 1, 20, 13, 0, 0, 0, "text", 0);*/
	}
}

extern "C" _declspec(dllexport) void DealClickedSetLM(CallBackFun pFun)
{
	if (IsSetLMClicked&&pFun)
	{
		int x = (int)(pval[0] * 1000);
		int y = (int)(pval[1] * 1000);
		/*float minDis = 100000;
		int tarLMID = -1;
		int xSize = mapSetLMX.size();
		if (xSize > 0)
		{
		for (int i = 0; i < xSize; i++)
		{
		int x_temp = mapSetLMX[SetLandMark_ID[i]];
		int y_temp = mapSetLMY[SetLandMark_ID[i]];
		if (x >= x_temp - 600 && x <= x_temp + 600 && y >= y_temp - 600 && y <= y_temp + 600)
		{
		float dis_temp = sqrt(((x_temp - x)*(x_temp - x) + (y_temp - y)*(y_temp - y))*1.0);
		if (dis_temp < minDis)
		{
		minDis = dis_temp;
		tarLMID = SetLandMark_ID[i];
		}
		}
		}
		}*/
		pFun(x, y);
		IsSetLMClicked = FALSE;
	}
}

void wheelMouseCallback(const pcl::visualization::MouseEvent& event)
{
	if (event.getType() == 4 || (event.getType() == 5))    //4是滚轮向后，5是滚轮向前
	{
		double ScaleValue = win->GetRenderers()->GetFirstRenderer()->GetActiveCamera()->GetParallelScale();

		if (ScaleValue >= 45)
		{
			if (g_ChangeShapesInfo.ChangeSizeType != 0)
			{
				g_ChangeShapesInfo.ChangeSizeType = 0;
				g_ChangeShapesInfo.ChangeShapesFlg = true;
			}
		}
		else if (ScaleValue >= 20)
		{
			if (g_ChangeShapesInfo.ChangeSizeType != 1)
			{
				g_ChangeShapesInfo.ChangeSizeType = 1;
				g_ChangeShapesInfo.ChangeShapesFlg = true;
			}
		}
		else
		{
			if (g_ChangeShapesInfo.ChangeSizeType != 2)
			{
				g_ChangeShapesInfo.ChangeSizeType = 2;
				g_ChangeShapesInfo.ChangeShapesFlg = true;
			}
		}

		if (g_ChangeShapesInfo.ChangeShapesFlg == true)         //图中的xy的缩放对应为  圆柱体：x,z  正方体：x,y  面：x,y
		{
			string shapeName = "";
			if (g_ChangeShapesInfo.ChangeSizeType == 0)  //大尺寸
			{
				for (int i = 0; i < LandMark_cloud_ptr->size(); i++)
				{
					shapeName = "landmark" + to_string((_Longlong)(i));
					if (lastLMType == 0)    //0：圆柱体 
					{
						viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
					}
					else if (lastLMType == 1 || (lastLMType == 2))   //1：正方体、面
					{
						viewer->updateMyShapesScale(1.8, 1.8, 1, shapeName);
					}
				}
				for (int i = 0; i < SetLandMark_cloud_ptr->size(); i++)
				{
					shapeName = "Setlandmark" + to_string((_Longlong)(SetLandMark_ID.at(i)));
					if (lastLMType == 0)    //0：圆柱体 
					{
						viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
					}
					else if (lastLMType == 1 || (lastLMType == 2))   //1：正方体、面
					{
						viewer->updateMyShapesScale(1.8, 1.8, 1, shapeName);
					}
				}
				shapeName = "LaserBody";
				viewer->updateMyShapesScale(1.8, 1.8, 1.8, shapeName);
				shapeName = "LaserAngle";
				viewer->updateMyLaserAngle(1.8, LastLaserPose[1], LastLaserPose[2], LastLaserPose[3], shapeName);
			}
			else if (g_ChangeShapesInfo.ChangeSizeType == 1)   //中尺寸
			{
				for (int i = 0; i < LandMark_cloud_ptr->size(); i++)
				{
					shapeName = "landmark" + to_string((_Longlong)(i));
					if (lastLMType == 0)    //0：圆柱体 
					{
						viewer->updateMyShapesScale(1, 1, 1, shapeName);
					}
					else if (lastLMType == 1 || (lastLMType == 2))   //1：正方体、面
					{
						viewer->updateMyShapesScale(1, 1, 1, shapeName);
					}
				}
				for (int i = 0; i < SetLandMark_cloud_ptr->size(); i++)
				{
					shapeName = "Setlandmark" + to_string((_Longlong)(SetLandMark_ID.at(i)));
					if (lastLMType == 0)    //0：圆柱体 
					{
						viewer->updateMyShapesScale(1, 1, 1, shapeName);
					}
					else if (lastLMType == 1 || (lastLMType == 2))   //1：正方体、面
					{
						viewer->updateMyShapesScale(1, 1, 1, shapeName);
					}
				}
				shapeName = "LaserBody";
				viewer->updateMyShapesScale(1, 1, 1, shapeName);
				shapeName = "LaserAngle";
				viewer->updateMyLaserAngle(1, LastLaserPose[1], LastLaserPose[2], LastLaserPose[3], shapeName);
			}
			else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
			{
				for (int i = 0; i < LandMark_cloud_ptr->size(); i++)
				{
					shapeName = "landmark" + to_string((_Longlong)(i));
					if (lastLMType == 0)    //0：圆柱体 
					{
						viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
					}
					else if (lastLMType == 1 || (lastLMType == 2))   //1：正方体、面
					{
						viewer->updateMyShapesScale(0.5, 0.5, 1, shapeName);
					}
				}
				for (int i = 0; i < SetLandMark_cloud_ptr->size(); i++)
				{
					shapeName = "Setlandmark" + to_string((_Longlong)(SetLandMark_ID.at(i)));
					if (lastLMType == 0)    //0：圆柱体 
					{
						viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
					}
					else if (lastLMType == 1 || (lastLMType == 2))   //1：正方体、面
					{
						viewer->updateMyShapesScale(0.5, 0.5, 1, shapeName);
					}
				}
				shapeName = "LaserBody";
				viewer->updateMyShapesScale(0.5, 0.5, 0.5, shapeName);
				shapeName = "LaserAngle";
				viewer->updateMyLaserAngle(0.5, LastLaserPose[1], LastLaserPose[2], LastLaserPose[3], shapeName);
			}
			g_ChangeShapesInfo.ChangeShapesFlg = false;
		}
	}
}

void TransView(vtkRenderer* render, float xTrans, float yTrans)
{
	if (render == NULL)
	{
		return;
	}
	double viewFocus[4], focalDepth, viewPoint[3];
	double newPickPoint[4], oldPickPoint[4], motionVector[3];

	// Calculate the focal depth since we'll be using it a lot

	vtkCamera *camera = render->GetActiveCamera();
	camera->GetFocalPoint(viewFocus);
	render->SetWorldPoint(viewFocus[0], viewFocus[1], viewFocus[2], viewFocus[3]);
	render->WorldToDisplay();
	viewFocus[0] = render->GetDisplayPoint()[0];
	viewFocus[1] = render->GetDisplayPoint()[1];
	viewFocus[2] = render->GetDisplayPoint()[2];
	viewFocus[3] = render->GetDisplayPoint()[3];
	focalDepth = viewFocus[2];

	double x[2];
	x[0] = win->GetSize()[0];
	x[1] = win->GetSize()[1];
	double x1 = x[0] / 2, y1 = x[1] / 2;
	// Has to recalc old mouse point since the viewport has moved,
	// so can't move it outside the loop
	render->SetDisplayPoint(x1, y1, focalDepth);
	render->DisplayToWorld();
	oldPickPoint[0] = render->GetWorldPoint()[0];
	oldPickPoint[1] = render->GetWorldPoint()[1];
	oldPickPoint[2] = render->GetWorldPoint()[2];
	oldPickPoint[3] = render->GetWorldPoint()[3];

	// Camera motion is reversed
	motionVector[0] = xTrans;
	motionVector[1] = yTrans;
	motionVector[2] = 0;

	camera->GetFocalPoint(viewFocus);
	camera->GetPosition(viewPoint);
	camera->SetFocalPoint(motionVector[0] + viewFocus[0],
		motionVector[1] + viewFocus[1],
		motionVector[2] + viewFocus[2]);

	camera->SetPosition(motionVector[0] + viewPoint[0],
		motionVector[1] + viewPoint[1],
		motionVector[2] + viewPoint[2]);

	if (render->GetLightFollowCamera())
	{
		render->UpdateLightsGeometryToFollowCamera();
	}
	win->Render();
}

void AddLaserLoPose(float *Pose)
{
	double XTranslation = 0;
	double YTranslation = 0;
	double Angle = 0;
	string shapeName = "";
	pcl::PointXYZ PCylinderDowm, PCylinderUp;

	if (int(*Pose) == 0)
	{
		Angle = 0;
		PCylinderDowm.x = 0;
		PCylinderDowm.y = 0;
	}
	else
	{
		Angle = *(Pose + 1);
		PCylinderDowm.x = *(Pose + 2) / 1000;
		PCylinderDowm.y = *(Pose + 3) / 1000;
	}
	PCylinderDowm.z = 0;

	if ((int)LastLaserPose[0] == -1)
	{
		shapeName = "LaserBody";
		viewer->addMyLaserBody(PCylinderDowm, shapeName, 0);
		shapeName = "LaserAngle";
		viewer->addMyLaserAngle(PCylinderDowm, Angle, shapeName, 0);
		LastLaserPose[0] = 0;
	}
	else
	{
		shapeName = "LaserBody";
		XTranslation = PCylinderDowm.x - LastLaserPose[2];
		YTranslation = PCylinderDowm.y - LastLaserPose[3];

		viewer->updateMyShapes(XTranslation, YTranslation, shapeName);
	}
	if (g_ChangeShapesInfo.ChangeSizeType == 0)  //大尺寸
	{
		viewer->updateMyShapesScale(1.8, 1.8, 1.8, shapeName);
	}
	else if (g_ChangeShapesInfo.ChangeSizeType == 1)   //中尺寸
	{
		viewer->updateMyShapesScale(1, 1, 1, shapeName);
	}
	else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
	{
		viewer->updateMyShapesScale(0.5, 0.5, 0.5, shapeName);
	}
	shapeName = "LaserAngle";
	if (Angle != LastLaserPose[1])
	{
		//删除
		viewer->removeShape(shapeName);
		//平移
		if (abs(XTranslation) > 1 || abs(YTranslation) > 1)//大于1m平移
		{
			/*vtkRenderer *ren = FindPokedRenderer((int)(*(Pose + 2) / 1000), (int)(*(Pose + 3) / 1000));
			TransView(ren, XTranslation, YTranslation);*/
			EqualProportionProjectionByXY(*(Pose + 2) / 1000, *(Pose + 3) / 1000);
		}
		//添加
		viewer->addMyLaserAngle(PCylinderDowm, Angle, shapeName, 0);
		if (g_ChangeShapesInfo.ChangeSizeType == 0)  //大尺寸
		{
			viewer->updateMyLaserAngle(1.8, Angle, PCylinderDowm.x, PCylinderDowm.y, shapeName);
		}
		if (g_ChangeShapesInfo.ChangeSizeType == 1)   //中尺寸
		{
			viewer->updateMyLaserAngle(1, Angle, PCylinderDowm.x, PCylinderDowm.y, shapeName);
		}
		else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
		{
			viewer->updateMyLaserAngle(0.5, Angle, PCylinderDowm.x, PCylinderDowm.y, shapeName);
		}
	}
	LastLaserPose[1] = Angle;
	LastLaserPose[2] = PCylinderDowm.x;
	LastLaserPose[3] = PCylinderDowm.y;
}

extern "C" _declspec(dllexport) void CreatPCLView(HWND hWnd)
{

	Tolerance = 0.025; // 1/40th of the renderer window

	MapperPosition[0] = 0.0;
	MapperPosition[1] = 0.0;
	MapperPosition[2] = 0.0;

	Mapper = NULL;
	DataSet = NULL;
	GlobalTMin = VTK_DOUBLE_MAX;
	Actors = vtkActorCollection::New();
	Prop3Ds = vtkProp3DCollection::New();
	PickedPositions = vtkPoints::New();
	Transform = vtkTransform::New();

	boost::shared_ptr<pcl::visualization::PCLVisualizer> temp_viewer(new pcl::visualization::PCLVisualizer("3D Viewer"));
	grid_actor_ = vtkLegendScaleActor::New();
	win = temp_viewer->getRenderWindow();
	iren = vtkRenderWindowInteractor::New();
	win->SetSize(200, 200);
	win->SetPosition(0, 0);
	iren->SetRenderWindow(win);
	win->SetParentId(hWnd);
	temp_viewer->createInteractor();

	win->DebugOff();

	temp_viewer->setBackgroundColor(0.94118, 0.97255, 1);
	temp_viewer->addCoordinateSystem(1.0);
	temp_viewer->initCameraParameters();

	viewer = temp_viewer;

	SetUpGrid(80, 5);//总步数 步长
	initVTKCamera();
	memset(&g_ChangeShapesInfo, 0, sizeof(g_ChangeShapesInfo));

	CreatPointCloudView(Laser_cloud_ptr);
	viewer->registerPointPickingCallback(ClickPointCallBack);
	viewer->registerMouseCallback(mouseEventOccurred);
	viewer->registerMouseCallback(wheelMouseCallback);
	win->Render();
}


extern "C" _declspec(dllexport) void ChangePCLView(int Width, int Height)
{
	if (win)
	{
		win->SetSize(Width, Height);
	}
}

extern "C" _declspec(dllexport) void ClearLMInfo()
{
	boost::lock_guard<boost::mutex> lock(m_ViewMutex);
	for (int num = 0; num < LandMark_cloud_ptr->size(); num++)
	{
		string shapeName = "landmark" + to_string((_Longlong)(num));
		viewer->removeShape(shapeName);
	}
	LandMark_cloud_ptr->clear();
	//删除点云
	Laser_cloud_ptr->clear();
	viewer->updatePointCloud(Laser_cloud_ptr, "sample cloud");
	viewer->spinOnce(10);
}

//删除显示的设置靶标
extern "C" _declspec(dllexport) void ClearSetLMInfo()
{
	boost::lock_guard<boost::mutex> lock(m_ViewMutex);
	for (int num = 0; num < SetLandMark_cloud_ptr->size(); num++)
	{
		string shapeName = "Setlandmark" + to_string((_Longlong)(SetLandMark_ID.at(num)));
		viewer->removeShape(shapeName);
	}
	SetLandMark_cloud_ptr->clear();
	SetLandMark_ID.clear();
}

//尺标显示
bool grid_enabled_ = false;
extern "C" _declspec(dllexport) void DisplayScaleGrid()
{
	vtkRenderer *CurrentRenderer = vtkRenderer::New();
	vtkAxisActor2D *m_AxisActor;
	CurrentRenderer = iren->FindPokedRenderer(NULL, NULL);
	if (!grid_enabled_)
	{
		grid_actor_->GetLegendLabelProperty()->SetColor(1, 0, 0);
		grid_actor_->GetLegendTitleProperty()->SetColor(1, 0, 0);

		//设置底部轴线
		m_AxisActor = grid_actor_->GetBottomAxis();
		m_AxisActor->GetProperty()->SetColor(1, 0, 0);
		m_AxisActor->GetLabelTextProperty()->SetColor(1, 0, 0);

		//设置顶部轴线
		m_AxisActor = grid_actor_->GetTopAxis();
		m_AxisActor->GetProperty()->SetColor(1, 0, 0);
		m_AxisActor->GetLabelTextProperty()->SetColor(1, 0, 0);

		//设置左部轴线
		m_AxisActor = grid_actor_->GetLeftAxis();
		m_AxisActor->GetProperty()->SetColor(1, 0, 0);
		m_AxisActor->GetLabelTextProperty()->SetColor(1, 0, 0);

		//设置右部轴线
		m_AxisActor = grid_actor_->GetRightAxis();
		m_AxisActor->GetProperty()->SetColor(1, 0, 0);
		m_AxisActor->GetLabelTextProperty()->SetColor(1, 0, 0);

		CurrentRenderer->AddViewProp(grid_actor_);
		grid_enabled_ = true;
	}
	else
	{
		CurrentRenderer->RemoveViewProp(grid_actor_);
		grid_enabled_ = false;
	}
	win->Render();
}

pcl::PointCloud<pcl::PointXYZ>::Ptr Laser1_cloud_ptr(new pcl::PointCloud<pcl::PointXYZ>);
extern "C" _declspec(dllexport) void UpdateCloudPoint(float x, float y, float z)
{
	uint8_t r(15), g(15), b(255);
	pcl::PointXYZRGB temp_point;
	temp_point.x = x;
	temp_point.y = y;
	temp_point.z = z;
	uint32_t rgb = (static_cast<uint32_t>(r) << 16 |
		static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
	temp_point.rgb = *reinterpret_cast<float*>(&rgb);
	Laser_cloud_ptr->points.push_back(temp_point);

	pcl::PointXYZ temp1_point;
	temp1_point.x = x;
	temp1_point.y = y;
	temp1_point.z = z;
	Laser1_cloud_ptr->points.push_back(temp1_point);
}

extern "C" _declspec(dllexport) void AddCloudPoint(float dist, float angle, float z)
{
	uint8_t r(15), g(15), b(255);
	pcl::PointXYZRGB temp_point;
	temp_point.x = dist*cosf(pcl::deg2rad(angle));
	temp_point.y = dist*sinf(pcl::deg2rad(angle));
	temp_point.z = z;
	uint32_t rgb = (static_cast<uint32_t>(r) << 16 |
		static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
	temp_point.rgb = *reinterpret_cast<float*>(&rgb);
	Laser_cloud_ptr->points.push_back(temp_point);

}

extern "C" _declspec(dllexport) void RedLMDisplay(int ExeType, int LMNO)   //ExeType使用类型，0为下次显示更新，1为立即更新
{
	string shapeName = "";
	if (ExeType == 0)
	{
		CurrentRedLMNO = LMNO;
	}
	else
	{
		if (LMNO != -1)
		{
			if (CurrentRedLMNO != LMNO)
			{
				if (CurrentRedLMNO != -1)
				{
					shapeName = "landmark" + to_string((_Longlong)(CurrentRedLMNO));
					if (EveryLMFlag[CurrentRedLMNO] - 10 == 0)
					{
						viewer->updateMyShapeColor(0.176, 0.867, 0.867, shapeName);
					}
					else
					{
						viewer->updateMyShapeColor(0.176, 0.867, 0.176, shapeName);
					}
				}

				shapeName = "landmark" + to_string((_Longlong)(LMNO));
				viewer->updateMyShapeColor(0.867, 0.176, 0.176, shapeName);
				CurrentRedLMNO = LMNO;
			}
		}
		else
		{
			if (CurrentRedLMNO != -1)
			{
				shapeName = "landmark" + to_string((_Longlong)(CurrentRedLMNO));
				if (EveryLMFlag[CurrentRedLMNO] - 10 == 0)
				{
					viewer->updateMyShapeColor(0.176, 0.867, 0.867, shapeName);
				}
				else
				{
					viewer->updateMyShapeColor(0.176, 0.867, 0.176, shapeName);
				}
			}
			CurrentRedLMNO = -1;
		}
		viewer->spinOnce(100);
	}
}

int AddLandMarkArray(float *LMData, int LMCount)
{
	int LastLandMarkCount = LandMark_cloud_ptr->size();
	int TempLMCount = max(LMCount, LastLandMarkCount);
	double XTranslation = 0;
	double YTranslation = 0;
	int CurrentLMFlag = 0;
	double Angle = 0;
	string shapeName = "";
	pcl::PointXYZ PCylinderDowm, PCylinderUp;
	if (mapScanLMXY.size() > 0)
	{
		mapScanLMXY.clear();
	}
	for (int num = 0; num < TempLMCount; num++)
	{
		shapeName = "landmark" + to_string((_Longlong)(num));
		CurrentLMFlag = *(LMData + num * 3);	//当前靶标类型，0：未设置；1：已设置
		PCylinderDowm.x = *(LMData + 1 + num * 3);
		PCylinderDowm.y = *(LMData + 2 + num * 3);
		mapScanLMXY.insert(pair<int, pcl::PointXYZ>(num, PCylinderDowm));
		PCylinderDowm.z = 0;
		if (num <= (LastLandMarkCount - 1) && num <= (LMCount - 1))
		{
			XTranslation = PCylinderDowm.x - LandMark_cloud_ptr->at(num).x;
			YTranslation = PCylinderDowm.y - LandMark_cloud_ptr->at(num).y;
			if (XTranslation >= 0.05 || XTranslation <= -0.05 || YTranslation >= 0.05 || YTranslation <= -0.05)
			{
				viewer->updateMyShapes(XTranslation, YTranslation, shapeName);
				LandMark_cloud_ptr->at(num).x = PCylinderDowm.x;
				LandMark_cloud_ptr->at(num).y = PCylinderDowm.y;
				LandMark_cloud_ptr->at(num).z = 0;
			}
			//颜色改变
			if (CurrentRedLMNO == num)
			{
				viewer->updateMyShapeColor(0.867, 0.176, 0.176, shapeName);
				EveryLMFlag[num] = CurrentLMFlag + 10;   //记录标红的靶标类型，为原类型+10
			}
			else
			{
				if (CurrentLMFlag != EveryLMFlag[num])
				{
					//改变颜色
					EveryLMFlag[num] = CurrentLMFlag;
					if (CurrentLMFlag == 0)   //0设置的靶标；1匹配上的靶标；2新添加的靶标
					{
						viewer->updateMyShapeColor(0.176, 0.867, 0.867, shapeName);
					}
					else if (CurrentLMFlag == 1)
					{
						viewer->updateMyShapeColor(0.176, 0.867, 0.176, shapeName);
					}
				}
			}
		}
		else
		{
			if (num >= LastLandMarkCount)
			{
				PCylinderUp.x = PCylinderDowm.x;
				PCylinderUp.y = PCylinderDowm.y;
				PCylinderUp.z = 5;
				//创建靶标
				if (g_ChangeShapesInfo.ChangeSizeType == 0)   //大尺寸
				{
					viewer->addMyCylinder(PCylinderDowm, PCylinderUp, 0.6, 0.176, 0.867, 0.867, shapeName, 0);//浅绿色
					viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
				}
				else if (g_ChangeShapesInfo.ChangeSizeType == 1)                //中尺寸
				{
					viewer->addMyCylinder(PCylinderDowm, PCylinderUp, 0.6, 0.176, 0.867, 0.867, shapeName, 0);
				}
				else if (g_ChangeShapesInfo.ChangeSizeType == 2)               //小尺寸
				{
					viewer->addMyCylinder(PCylinderDowm, PCylinderUp, 0.6, 0.176, 0.867, 0.867, shapeName, 0);
					viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
				}
				//改变颜色
				EveryLMFlag[num] = CurrentLMFlag;
				if (CurrentRedLMNO == num)
				{
					viewer->updateMyShapeColor(0.867, 0.176, 0.176, shapeName);
					EveryLMFlag[num] = CurrentLMFlag + 10;   //记录标红的靶标类型，为原类型+10
				}
				else
				{
					if (CurrentLMFlag == 1)   //改颜色
					{
						viewer->updateMyShapeColor(0.176, 0.867, 0.176, shapeName);
					}
				}
				LandMark_cloud_ptr->push_back(PCylinderDowm);
			}
			else
			{
				//删除
				viewer->removeShape(shapeName);
				LandMark_cloud_ptr->erase(LandMark_cloud_ptr->end() - 1);
			}
		}
	}
	int tarLMID = -1;
	if (IsScanLMClicked)
	{
		IsScanLMClicked = FALSE;
		int x = (int)(pval[0] * 1000);
		int y = (int)(pval[1] * 1000);
		float minDis = 100000;
		int scanLMSize = mapScanLMXY.size();
		if (scanLMSize > 0)
		{
			for (int i = 0; i < scanLMSize; i++)
			{
				int x_temp = (int)(mapScanLMXY[i].x * 1000);
				int y_temp = (int)(mapScanLMXY[i].y * 1000);
				if (x >= x_temp - 600 && x <= x_temp + 600 && y >= y_temp - 600 && y <= y_temp + 600)
				{
					float dis_temp = sqrt(((x_temp - x)*(x_temp - x) + (y_temp - y)*(y_temp - y))*1.0);
					if (dis_temp < minDis)
					{
						minDis = dis_temp;
						tarLMID = i;
					}
				}
			}
		}
	}
	return tarLMID;
}
string addShapename[LMColorArrayCount] = { "" };//靶标添加的靶标名字
int addLMCount = 0;//靶标添加的靶标个数

string addLayerShapename[LMColorArrayCount] = { "" };//靶标添加的靶标名字
int addLayerLMCount = 0;//靶标添加的靶标个数
extern "C" _declspec(dllexport) void RedSetLMDisplay(int LMID)
{
	viewer->removePointCloud("Rightclicked_points");
	string shapeName = "";
	if (LMID != -1)
	{
		if (CurrentRedSetLMID != LMID)
		{
			if (CurrentRedSetLMID != -1)
			{
				bool updateBlueColorFlag = false;
				shapeName = "Setlandmark" + to_string((_Longlong)(CurrentRedSetLMID));
				if (addLMCount > 0)
				{
					for (int i = 0; i < addLMCount; i++)
					{
						if (addShapename[i] == shapeName)
						{
							viewer->updateMyShapeColor(0, 0, 255, shapeName);
							updateBlueColorFlag = true;
							break;
						}
					}
				}
				if (!updateBlueColorFlag)
				{
					viewer->updateMyShapeColor(0, 0, 0, shapeName);
				}
				//shapeName = "Setlandmark" + to_string((_Longlong)(CurrentRedSetLMID));
				//viewer->updateMyShapeColor(0, 0, 0, shapeName);
			}
			shapeName = "Setlandmark" + to_string((_Longlong)(LMID));
			viewer->updateMyShapeColor(0.867, 0.176, 0.176, shapeName);
			CurrentRedSetLMID = LMID;
		}
	}
	else
	{
		if (CurrentRedSetLMID != -1)
		{
			bool updateBlueColorFlag = false;
			shapeName = "Setlandmark" + to_string((_Longlong)(CurrentRedSetLMID));
			if (addLMCount > 0)
			{
				for (int i = 0; i < addLMCount; i++)
				{
					if (addShapename[i] == shapeName)
					{
						viewer->updateMyShapeColor(0, 0, 255, shapeName);
						updateBlueColorFlag = true;
						break;
					}
				}
			}
			if (!updateBlueColorFlag)
			{
				viewer->updateMyShapeColor(0, 0, 0, shapeName);
			}
		}
		CurrentRedSetLMID = -1;
	}
	viewer->spinOnce(100);
}

//添加or修改，显示当前设置或者要设置的靶标
extern "C" _declspec(dllexport) void AddOneSetLMDisplay(int x, int y, int LMID)
{
	string shapeName = "";
	shapeName = "Setlandmark" + to_string((_Longlong)(LMID));
	viewer->removePointCloud("ShiftLeftclicked_points");
	int LastSetLandmarkCount = SetLandMark_cloud_ptr->size();
	double XTranslation = 0;
	double YTranslation = 0;
	pcl::PointXYZ PCylinderDowm, PCylinderUp;
	shapeName = "Setlandmark" + to_string((_Longlong)(LMID));
	PCylinderDowm.x = ((float)x) / 1000;
	PCylinderDowm.y = ((float)y) / 1000;
	PCylinderDowm.z = 0;
	int endItemID = 0;
	if (SetLandMark_ID.empty())
	{
		endItemID = -1;
	}
	else
	{
		endItemID = SetLandMark_ID.back();
	}

	if (endItemID < LMID)
	{
		PCylinderUp.x = PCylinderDowm.x;
		PCylinderUp.y = PCylinderDowm.y;
		PCylinderUp.z = 5;
		viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
		if (g_ChangeShapesInfo.ChangeSizeType == 0)   //大尺寸
		{
			viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
		}
		else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
		{
			viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
		}
		SetLandMark_cloud_ptr->push_back(PCylinderDowm);
		SetLandMark_ID.push_back(LMID);
	}
	else
	{
		for (int i = 0; i<LastSetLandmarkCount; i++)
		{
			if (SetLandMark_ID.at(i)>LMID)
			{
				PCylinderUp.x = PCylinderDowm.x;
				PCylinderUp.y = PCylinderDowm.y;
				PCylinderUp.z = 5;
				viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
				if (g_ChangeShapesInfo.ChangeSizeType == 0)   //大尺寸
				{
					viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
				}
				else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
				{
					viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
				}
				SetLandMark_cloud_ptr->insert(SetLandMark_cloud_ptr->begin() + i, PCylinderDowm);
				SetLandMark_ID.insert(SetLandMark_ID.begin() + i, LMID);
				break;
			}
			else if (SetLandMark_ID.at(i) == LMID)
			{
				XTranslation = PCylinderDowm.x - SetLandMark_cloud_ptr->at(i).x;
				YTranslation = PCylinderDowm.y - SetLandMark_cloud_ptr->at(i).y;
				if (XTranslation != 0 || (YTranslation != 0))
				{
					viewer->updateMyShapes(XTranslation, YTranslation, shapeName);
					SetLandMark_cloud_ptr->at(i).x = PCylinderDowm.x;
					SetLandMark_cloud_ptr->at(i).y = PCylinderDowm.y;
					SetLandMark_cloud_ptr->at(i).z = 0;
				}
				break;
			}
			else
			{
				continue;
			}
		}
	}
	viewer->spinOnce(10);
}
//删除，显示当前设置或者要设置的靶标
extern "C" _declspec(dllexport) void DeleteOneSetLMDisplay(int LMID)
{
	int LastSetLandmarkCount = SetLandMark_cloud_ptr->size();
	string shapeName = "Setlandmark" + to_string((_Longlong)(LMID));
	viewer->removeShape(shapeName);
	for (int i = 0; i < LastSetLandmarkCount; i++)
	{
		if (SetLandMark_ID.at(i) == LMID)
		{
			SetLandMark_cloud_ptr->erase(SetLandMark_cloud_ptr->begin() + i);
			SetLandMark_ID.erase(SetLandMark_ID.begin() + i);
			break;
		}
	}
	viewer->spinOnce(10);
}

//显示当前设置或者要设置的靶标，传入的数据要求按照ID升序排序
extern "C" _declspec(dllexport) void SetLMArrayDisplay(int x[12000], int y[12000], int LMID[12000], int BlueColorFlag[], int LMCount, int SetLMKind, bool f_LayerChange = false)
{
	string shapeName = "";
	if (addLMCount > 0)
	{
		for (int i = 0; i < addLMCount; i++)
		{
			viewer->updateMyShapeColor(0, 0, 0, addShapename[i]);//把上次添加的靶标颜色设置为黑色
		}
		addLMCount = 0;
	}
	viewer->removePointCloud("ShiftLeftclicked_points");
	int LastSetLandmarkCount = SetLandMark_cloud_ptr->size();
	int CurrentLMFlag = 0;
	double XTranslation = 0;
	double YTranslation = 0;
	pcl::PointXYZ PCylinderDowm, PCylinderUp;

	//清除点云
	Laser_cloud_ptr->clear();
	viewer->updatePointCloud(Laser_cloud_ptr, "sample cloud");

	float m_LaserPose[4];
	m_LaserPose[0] = 0;
	m_LaserPose[1] = 0;
	m_LaserPose[2] = 0;
	m_LaserPose[3] = 0;

	boost::lock_guard<boost::mutex> lock(m_ViewMutex);
	AddLaserLoPose(m_LaserPose);

	//删除当前实时获取的靶标
	for (int num = 0; num < LandMark_cloud_ptr->size(); num++)
	{
		shapeName = "landmark" + to_string((_Longlong)(num));
		viewer->removeShape(shapeName);
	}
	LandMark_cloud_ptr->clear();

	//显示靶标
	int i = 0;
	int j = 0;
	for (; i < LMCount; i++)
	{
		int blueFlag = 0;
		shapeName = "Setlandmark" + to_string((_Longlong)(LMID[i]));
		PCylinderDowm.x = ((float)x[i]) / 1000;
		PCylinderDowm.y = ((float)y[i]) / 1000;
		PCylinderDowm.z = 0;
		blueFlag = BlueColorFlag[i];
		if (j >= LastSetLandmarkCount)
		{
			PCylinderUp.x = PCylinderDowm.x;
			PCylinderUp.y = PCylinderDowm.y;
			PCylinderUp.z = 5;
			if (SetLMKind == 1)
			{
				viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
			}
			else
			{
				if (blueFlag)
				{
					if (addLMCount >= LMColorArrayCount - 1)//防止数组越界
					{
						addLMCount = 0;
					}
					viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 255);//添加的靶标用蓝色显示
					addShapename[addLMCount] = shapeName;
					addLMCount++;
				}
				else
				{
					viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
				}
			}
			if (g_ChangeShapesInfo.ChangeSizeType == 0)   //大尺寸
			{
				viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
			}
			else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
			{
				viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
			}
			SetLandMark_cloud_ptr->push_back(PCylinderDowm);
			SetLandMark_ID.push_back(LMID[i]);
		}
		else
		{
			for (; j < LastSetLandmarkCount;)
			{
				if (SetLandMark_ID.at(j) == LMID[i])
				{
					XTranslation = PCylinderDowm.x - SetLandMark_cloud_ptr->at(j).x;
					YTranslation = PCylinderDowm.y - SetLandMark_cloud_ptr->at(j).y;
					if (XTranslation != 0 || (YTranslation != 0))
					{
						viewer->updateMyShapes(XTranslation, YTranslation, shapeName);
						SetLandMark_cloud_ptr->at(j).x = PCylinderDowm.x;
						SetLandMark_cloud_ptr->at(j).y = PCylinderDowm.y;
						SetLandMark_cloud_ptr->at(j).z = 0;
					}
					if (f_LayerChange&&blueFlag)
					{
						viewer->removeShape(shapeName);
						viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 255);
						viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
					}
					j++;
					break;
				}
				else if (SetLandMark_ID.at(j) > LMID[i])
				{
					PCylinderUp.x = PCylinderDowm.x;
					PCylinderUp.y = PCylinderDowm.y;
					PCylinderUp.z = 5;
					if (SetLMKind == 1)
					{
						viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
					}
					else
					{
						if (blueFlag)
						{
							if (addLMCount >= LMColorArrayCount - 1)//防止数组越界
							{
								addLMCount = 0;
							}
							viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 255);//添加的靶标用蓝色显示
							addShapename[addLMCount] = shapeName;
							addLMCount++;
						}
						else
						{
							viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);//添加的靶标用蓝色显示
						}
					}
					if (g_ChangeShapesInfo.ChangeSizeType == 0)   //大尺寸
					{
						viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
					}
					else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
					{
						viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
					}
					SetLandMark_cloud_ptr->insert(SetLandMark_cloud_ptr->begin() + j, PCylinderDowm);
					SetLandMark_ID.insert(SetLandMark_ID.begin() + j, LMID[i]);
					j++;
					LastSetLandmarkCount++;
					break;
				}
				else
				{
					//删除
					string deleteshapeName = "Setlandmark" + to_string((_Longlong)(SetLandMark_ID[j]));
					viewer->removeShape(deleteshapeName);
					SetLandMark_cloud_ptr->erase(SetLandMark_cloud_ptr->begin() + j);
					SetLandMark_ID.erase(SetLandMark_ID.begin() + j);
					LastSetLandmarkCount--;
					if (j == LastSetLandmarkCount)
					{
						PCylinderUp.x = PCylinderDowm.x;
						PCylinderUp.y = PCylinderDowm.y;
						PCylinderUp.z = 5;
						if (SetLMKind == 1)
						{
							viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
						}
						else
						{
							if (blueFlag)
							{
								if (addLMCount >= LMColorArrayCount - 1)//防止数组越界
								{
									addLMCount = 0;
								}
								viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 255);//添加的靶标用蓝色显示
								addShapename[addLMCount] = shapeName;
								addLMCount++;
							}
							else
							{
								viewer->addLMCylinder(PCylinderDowm, PCylinderUp, 0.6, shapeName, 0, 0, 0, 0);
							}
						}
						if (g_ChangeShapesInfo.ChangeSizeType == 0)   //大尺寸
						{
							viewer->updateMyShapesScale(1.8, 1, 1.8, shapeName);
						}
						else if (g_ChangeShapesInfo.ChangeSizeType == 2)   //小尺寸
						{
							viewer->updateMyShapesScale(0.5, 1, 0.5, shapeName);
						}
						SetLandMark_cloud_ptr->push_back(PCylinderDowm);
						SetLandMark_ID.push_back(LMID[i]);
					}
				}
			}
		}
	}
	//删除多余的靶标
	if (j < LastSetLandmarkCount)
	{
		for (; j < LastSetLandmarkCount; j++)
		{
			shapeName = "Setlandmark" + to_string((_Longlong)(SetLandMark_ID.at(SetLandMark_ID.size() - 1)));
			viewer->removeShape(shapeName);
			SetLandMark_cloud_ptr->erase(SetLandMark_cloud_ptr->end() - 1);
			SetLandMark_ID.pop_back();
		}
	}
	viewer->spinOnce(10);
}

extern "C" _declspec(dllexport) int AddCloudPointArray(float x[7200], float y[7200], float z[7200], float LandmarkData[121], float LaserPose[4], bool RSSIFlag, float RSSIData[7200], int ScanPointCount)
{
	int returnVal = -1;
	viewer->removePointCloud("Rightclicked_points");
	uint8_t r(15), g(15), b(255);
	pcl::PointXYZRGB temp_point;
	uint32_t rgb = (static_cast<uint32_t>(r) << 16 |
		static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
	temp_point.rgb = *reinterpret_cast<float*>(&rgb);

	boost::lock_guard<boost::mutex> lock(m_ViewMutex);
	Laser_cloud_ptr->clear();

	for (int i = 0; i < ScanPointCount; i++)
	{
		//根据能量值改变点云颜色
		if (RSSIFlag == true)
		{
			if (RSSIData[i] <= 120)
			{
				r = 15;
				g = 15 + RSSIData[i] * 240 / 120;
				b = 255 - RSSIData[i] * 240 / 120 * 1.3;
				if (b < 0)
				{
					b = 0;
				}
				rgb = (static_cast<uint32_t>(r) << 16 |
					static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
				temp_point.rgb = *reinterpret_cast<float*>(&rgb);
			}
			else if (RSSIData[i] < 256)
			{
				r = 255;
				g = 170 - RSSIData[i] * 155 / 255;
				if (g < 0)
				{
					g = 0;
				}
				b = 15;
				rgb = (static_cast<uint32_t>(r) << 16 |
					static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
				temp_point.rgb = *reinterpret_cast<float*>(&rgb);
			}
		}
		temp_point.x = x[i];
		temp_point.y = y[i];
		temp_point.z = z[i];
		Laser_cloud_ptr->points.push_back(temp_point);
	}
	Laser_cloud_ptr->width = (int)Laser_cloud_ptr->points.size();
	Laser_cloud_ptr->height = 1;

	//显示扫描数据
	if (FlagPointDisplay == true)
	{
		viewer->updatePointCloud(Laser_cloud_ptr, "sample cloud");
	}
	else
	{
		viewer->removeShape("PolyLine");
		viewer->addPolyLine<pcl::PointXYZRGB>(Laser_cloud_ptr, 0.06, 0.06, 1, "PolyLine");
	}
	//显示自身位置
	AddLaserLoPose(LaserPose);
	//显示靶标
	if (LandmarkData[0] != 0)
	{
		returnVal = AddLandMarkArray(&LandmarkData[1], (int)LandmarkData[0]);
	}
	else
	{
		if (LandMark_cloud_ptr->size() > 0)
		{
			string shapeName = "";
			for (int num = 0; num < LandMark_cloud_ptr->size(); num++)
			{
				shapeName = "landmark" + to_string((_Longlong)(num));
				viewer->removeShape(shapeName);
			}
			LandMark_cloud_ptr->clear();
		}
	}

	viewer->spinOnce(100);
	return returnVal;
}

extern "C" _declspec(dllexport) void AddScanPointArray(float x[7200], float y[7200], float LaserPose[4], int ScanPointCount)
{
	viewer->removePointCloud("Rightclicked_points");
	uint8_t r(15), g(15), b(255);
	pcl::PointXYZRGB temp_point;
	uint32_t rgb = (static_cast<uint32_t>(r) << 16 |
		static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
	temp_point.rgb = *reinterpret_cast<float*>(&rgb);

	boost::lock_guard<boost::mutex> lock(m_ViewMutex);
	//写入点云
	Laser_cloud_ptr->clear();

	for (int i = 0; i < ScanPointCount; i++)
	{
		temp_point.x = x[i];
		temp_point.y = y[i];
		temp_point.z = 1;
		Laser_cloud_ptr->points.push_back(temp_point);
	}
	Laser_cloud_ptr->width = (int)Laser_cloud_ptr->points.size();
	Laser_cloud_ptr->height = 1;

	//显示扫描数据
	if (FlagPointDisplay == true)
	{
		viewer->updatePointCloud(Laser_cloud_ptr, "sample cloud");
	}
	else
	{
		viewer->removeShape("PolyLine");
		viewer->addPolyLine<pcl::PointXYZRGB>(Laser_cloud_ptr, 0.06, 0.06, 1, "PolyLine");
	}
	//显示自身位置
	AddLaserLoPose(LaserPose);
	viewer->spinOnce(100);
}

extern "C" _declspec(dllexport) void UpdatePointCloudbyName()
{
	uint8_t r(255), g(15), b(15);
	for (float z(-1.0); z <= 1.0; z += 0.02)
	{
		for (float x(0.0); x <= 360.0; x += 0.5)
		{
			pcl::PointXYZRGB basic_point;
			basic_point.x = x;
			float randomcs = rand() % 10;
			basic_point.y = x * z * randomcs;
			basic_point.z = z;
			uint32_t rgb = (static_cast<uint32_t>(r) << 16 |
				static_cast<uint32_t>(g) << 8 | static_cast<uint32_t>(b));
			basic_point.rgb = *reinterpret_cast<float*>(&rgb);
			Laser_cloud_ptr->points.push_back(basic_point);
		}
		if (z < 0.0)
		{
			r -= 12;
			g += 12;
		}
		else
		{
			g -= 12;
			b += 12;
		}
	}
	boost::lock_guard<boost::mutex> lock(m_ViewMutex);

	Laser_cloud_ptr->width = (int)Laser_cloud_ptr->points.size();
	Laser_cloud_ptr->height = 1;
	pcl::visualization::PointCloudColorHandlerRGBField<pcl::PointXYZRGB> rgb(Laser_cloud_ptr);
	viewer->updatePointCloud(Laser_cloud_ptr, "sample cloud");
	Laser_cloud_ptr->clear();
	viewer->spinOnce();
}

extern "C" _declspec(dllexport) void ChangePointDisplay(bool flagPointDisplay)
{
	if (flagPointDisplay == true)
	{
		if (FlagPointDisplay == false)
		{
			pcl::visualization::PointCloudColorHandlerRGBField<pcl::PointXYZRGB> rgb(Laser_cloud_ptr);
			viewer->addPointCloud<pcl::PointXYZRGB>(Laser_cloud_ptr, rgb, "sample cloud");
			viewer->setPointCloudRenderingProperties(pcl::visualization::PCL_VISUALIZER_POINT_SIZE, 3, "sample cloud");
			viewer->removeShape("PolyLine");
		}
	}
	else
	{
		if (FlagPointDisplay == true)
		{
			viewer->addPolyLine<pcl::PointXYZRGB>(Laser_cloud_ptr, 0.06, 0.06, 1, "PolyLine");
			viewer->removePointCloud("sample cloud");
		}
	}
	viewer->spinOnce(10);
	FlagPointDisplay = flagPointDisplay;
}

//变换PCLvisualizer 视角，实质是变换VTK 相机参数
extern "C" _declspec(dllexport) void setViewTo(int axis, int sign)
{
	initVTKCamera();//导致每次都会变得一样大
	std::vector<pcl::visualization::Camera> camera;
	viewer->getCameras(camera);

	Eigen::Vector3d position(camera[0].pos);
	double norm = position.norm();

	if (axis == 0)  //X
	{
		camera[0].view[0] = 0;
		camera[0].view[1] = 0;
		camera[0].view[2] = 1;

		camera[0].pos[0] = norm*sign*(-1);
		camera[0].pos[1] = norm*sign*(-0);
		camera[0].pos[2] = norm*sign*(-0);
	}
	else if (axis == 1)  //Y
	{
		camera[0].view[0] = 0;
		camera[0].view[1] = 0;
		camera[0].view[2] = 1;

		camera[0].pos[0] = norm*sign*(-0);
		camera[0].pos[1] = norm*sign*(-1);
		camera[0].pos[2] = norm*sign*(-0);
	}
	else if (axis == 2)  //Z
	{
		camera[0].view[0] = 0;
		camera[0].view[1] = 1;
		camera[0].view[2] = 0;
		camera[0].pos[0] = norm*sign*(0);
		camera[0].pos[1] = norm*sign*(0);
		camera[0].pos[2] = norm*sign*(-1);
		camera[0].focal[0] = 0;
		camera[0].focal[1] = 0;
		camera[0].focal[2] = sign;
	}

	viewer->camera_ = camera[0];
	viewer->resetCamera();
	viewer->updateCamera();
	win->GetRenderers()->GetFirstRenderer()->GetActiveCamera()->SetParallelProjection(1);
	win->Render();
}


//旋转上向量，即摄像机正方向，相机的镜头朝向和位置沿着Z轴逆时针90度 主动旋转是指将向量逆时针围绕旋转轴所做出的旋转
extern "C" _declspec(dllexport) void RotationViewTo()
{
	std::vector<pcl::visualization::Camera> camera;
	viewer->getCameras(camera);
	Eigen::RowVector3d OldUpVector(camera[0].view);
	Eigen::RowVector3d OldPosition(camera[0].pos);
	double norm = OldPosition.norm();
	Eigen::Matrix3d RotationM;
	RotationM << 0, 1, 0,
		-1, 0, 0,
		0, 0, 1;

	Eigen::RowVector3d NewUpVector = OldUpVector*RotationM;
	Eigen::RowVector3d NewPosition = OldPosition*RotationM;
	camera[0].view[0] = NewUpVector(0);
	camera[0].view[1] = NewUpVector(1);
	camera[0].view[2] = NewUpVector(2);
	camera[0].pos[0] = 0;
	camera[0].pos[1] = 0;
	camera[0].pos[2] = norm;
	camera[0].focal[0] = 0;
	camera[0].focal[1] = 0;
	camera[0].focal[2] = -1;
	viewer->camera_ = camera[0];
	viewer->updateCamera();
	win->Render();
}

//等比例投影XOY平面，视角为Z-
extern "C" _declspec(dllexport) void EqualProportionProjection()
{
	std::vector<pcl::visualization::Camera> camera;
	viewer->getCameras(camera);
	Eigen::Vector3d position(camera[0].pos);
	double norm = position.norm();
	camera[0].view[0] = 0;
	camera[0].view[1] = 1;
	camera[0].view[2] = 0;
	camera[0].pos[0] = pval[0];
	camera[0].pos[1] = pval[1];
	camera[0].pos[2] = norm;
	camera[0].focal[0] = pval[0];
	camera[0].focal[1] = pval[1];
	camera[0].focal[2] = -1;
	viewer->camera_ = camera[0];
	viewer->updateCamera();
	win->GetRenderers()->GetFirstRenderer()->GetActiveCamera()->SetParallelProjection(1);
	win->Render();
}

