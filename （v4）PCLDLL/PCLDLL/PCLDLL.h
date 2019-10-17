#include "vtkPointPicker.h"
class PCLDLL : public vtkAbstractPropPicker
{
public:
	void SelfInitialize();
	double IntersectWithLine(double p1[3], double p2[3],
		double vtkNotUsed(tol),
		vtkAssemblyPath *path,
		vtkProp3D *prop3D,
		vtkAbstractMapper3D *mapper);
	void MarkPicked(vtkAssemblyPath *path, vtkProp3D *prop3D, vtkAbstractMapper3D *m,
		double tMin, double mapperPos[3]);
	int Pick(double selectionX, double selectionY, double selectionZ, vtkRenderer *renderer);
};