#pragma once

namespace NativeFaceDetector
{

	class RectFeature
	{
		int m_x1, m_x2, m_y1, m_y2;
        float m_weight;

		int m_scaledLeft, m_scaledRight, m_scaledTop, m_scaledBottom;
	public:
		RectFeature(int x1 = 0, int x2 = 0, int y1 = 0, int y2 = 0, float weight = 0.0f);

		int scaledLeft() const { return m_scaledLeft; }
		int scaledRight() const { return m_scaledRight; }
		int scaledTop() const { return m_scaledTop; }
		int scaledBottom() const { return m_scaledBottom; }
		float weight() const { return m_weight; }

		void setScale(float scale, float invArea)
		{
			m_scaledLeft = (int)(scale * m_x1);
			m_scaledRight = (int)(scale * (m_x1 + m_y1));
			m_scaledTop = (int)(scale * m_x2);
			m_scaledBottom = (int)(scale * (m_x2 + m_y2));
		}
	};

}