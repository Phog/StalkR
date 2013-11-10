#include "pch.h"
#include "Feature.h"

#include <cmath>
#include <algorithm>

using namespace NativeFaceDetector;

Feature::Feature(float threshold, float leftVal, int leftNode, float rightVal, int rightNode, Size size)
	: m_size(size), m_threshold(threshold), m_leftVal(leftVal), m_rightVal(rightVal),
	m_leftNode(leftNode), m_rightNode(rightNode), m_numRects(0)
{
}

bool Feature::chooseLeft(const Matrix &grayImage, const Matrix &squares, int i, int j) const
{
	int total        = grayImage(i + m_width, j + m_height) + grayImage(i, j)
		             - grayImage(i, j + m_height) - grayImage(i + m_width, j);
	int squaredTotal = squares(i + m_width, j + m_height) + squares(i, j)
		             - squares(i, j + m_height) - squares(i + m_width, j);
	float mean     = total * m_invArea;
	float variance = squaredTotal * m_invArea - mean * mean;
	variance = (variance > 1.0f) ? sqrt(variance) : 1.0f;

	float rectSum = (grayImage(i + m_rects[0].scaledRight(), j + m_rects[0].scaledBottom()) 
		           - grayImage(i + m_rects[0].scaledLeft(), j + m_rects[0].scaledBottom())
		           - grayImage(i + m_rects[0].scaledRight(), j + m_rects[0].scaledTop())
				   + grayImage(i + m_rects[0].scaledLeft(), j + m_rects[0].scaledTop())) * m_rects[0].weight();
	if (m_numRects > 1)
		rectSum += (grayImage(i + m_rects[1].scaledRight(), j + m_rects[1].scaledBottom()) 
		           - grayImage(i + m_rects[1].scaledLeft(), j + m_rects[1].scaledBottom())
		           - grayImage(i + m_rects[1].scaledRight(), j + m_rects[1].scaledTop())
				   + grayImage(i + m_rects[1].scaledLeft(), j + m_rects[1].scaledTop())) * m_rects[1].weight();
	if (m_numRects > 2)
		rectSum += (grayImage(i + m_rects[2].scaledRight(), j + m_rects[2].scaledBottom()) 
		           - grayImage(i + m_rects[2].scaledLeft(), j + m_rects[2].scaledBottom())
		           - grayImage(i + m_rects[2].scaledRight(), j + m_rects[2].scaledTop())
				   + grayImage(i + m_rects[2].scaledLeft(), j + m_rects[2].scaledTop())) * m_rects[2].weight();

	return rectSum * m_invArea < m_threshold * variance;
}