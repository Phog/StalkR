#pragma once

#include "Matrix.h"

namespace NativeFaceDetector
{

	class RectFeature
	{
		int m_x1, m_x2, m_y1, m_y2;
        float m_weight;

		int m_scaledLeft, m_scaledRight, m_scaledTop, m_scaledBottom;
	public:
		RectFeature(int x1 = 0, int x2 = 0, int y1 = 0, int y2 = 0, float weight = 0.0f);

		void setScale(float scale)
		{
			m_scaledLeft = (int)(scale * m_x1);
			m_scaledRight = (int)(scale * (m_x1 + m_y1));
			m_scaledTop = (int)(scale * m_x2);
			m_scaledBottom = (int)(scale * (m_x2 + m_y2));
		}

		float weightedSum(const Matrix &grayImage, int i, int j) const
		{
			const int top    = i + m_scaledTop;
			const int bottom = i + m_scaledBottom;
			const int left   = j + m_scaledLeft;
			const int right  = j + m_scaledRight;

			return m_weight * (grayImage(bottom, right) - grayImage(bottom, left) -
						       grayImage(top, right)    + grayImage(top, left));
		}
	};

}