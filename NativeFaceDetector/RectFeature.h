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
		void setScale(float scale);

		float weightedSum(const Matrix &grayImage, int i, int j) const
		{
			return m_weight * grayImage.rectangleSum(i + m_scaledTop, j + m_scaledRight,
				                                     i + m_scaledBottom, j + m_scaledLeft);
		}
	};

}