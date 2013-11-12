#pragma once

#include "RectFeature.h"

#include "Matrix.h"
#include "Structures.h"

namespace NativeFaceDetector
{

	class Feature
	{
		RectFeature m_rects[3];
        Size m_size;

		float m_threshold, m_leftVal, m_rightVal;
		int m_leftNode, m_rightNode, m_numRects;

		float m_scale, m_invArea, m_scaledThreshold;
		int m_width, m_height;
	public:
        Feature(float threshold, float leftVal, int leftNode, float rightVal, int rightNode, Size size);
		bool chooseLeft(const Matrix &grayImage, const Matrix &squares, int i, int j) const;

		void setScale(float scale)
		{
			m_scale   = scale;
			m_width   = (int)(scale * m_size.width);
			m_height  = (int)(scale * m_size.height);

			const float area  = (float)(m_width * m_height);
			m_invArea         = 1.0f / area;
			m_scaledThreshold = m_threshold * area;
			
			for (int k = 0; k < m_numRects; k++)
				m_rects[k].setScale(scale);
		}

		void add(const RectFeature &r) { m_rects[m_numRects++] = r; }

		float leftVal() const { return m_leftVal; }
		float rightVal() const { return m_rightVal; }

		int leftNode() const { return m_leftNode; }
		int rightNode() const { return m_rightNode; }
	};

}
