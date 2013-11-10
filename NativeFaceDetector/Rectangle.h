#pragma once

#include <cmath>

namespace NativeFaceDetector
{

    public ref class Rectangle sealed
	{
		double m_x, m_y, m_width, m_height;
	public:
		Rectangle(double _x, double _y, double _w, double _h);

		double x() { return m_x; }
		double y() { return m_y; }
		double width() { return m_width; }
		double height() { return m_height; }

		void x(double _x) { m_x = _x; }
		void y(double _y) { m_y = _y; }
		void width(double _width) { m_width = _width; }
		void height(double _height) { m_height = _height; }

		bool contains(double _x, double _y);
		bool approximatelyEqual(Rectangle ^rhs);
		double distanceTo(Rectangle ^rhs);
		double diagonal() {	return sqrt(m_width * m_width + m_height * m_height); }
	};

}
