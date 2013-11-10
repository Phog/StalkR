#include "pch.h"
#include "Rectangle.h"

using namespace NativeFaceDetector;

Rectangle::Rectangle(double _x, double _y, double _w, double _h)
	: m_x(_x), m_y(_y), m_width(_w), m_height(_h)
{
}

bool Rectangle::approximatelyEqual(Rectangle ^rhs)
{
	int distance = (int)(width() * 0.2);

	if (rhs->x() <= x() + distance &&
		rhs->x() >= x() - distance &&
		rhs->y() <= y() + distance &&
		rhs->y() >= y() - distance &&
		rhs->width() <= (int)(width() * 1.2) &&
		(int)(rhs->width() * 1.2) >= width())
		return true;

	if (x() >= rhs->x() && x() + width() <= rhs->x() + rhs->width() && 
		y() >= rhs->y() && y() + height() <= rhs->y() + rhs->height()) 
		return true;

	return false;
}