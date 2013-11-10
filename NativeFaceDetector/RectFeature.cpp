#include "pch.h"
#include "RectFeature.h"

using namespace NativeFaceDetector;

RectFeature::RectFeature(int x1, int x2, int y1, int y2, float weight)
	: m_x1(x1), m_x2(x2), m_y1(y1), m_y2(y2), m_weight(weight)
{
}
