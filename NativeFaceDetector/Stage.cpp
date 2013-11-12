#include "pch.h"
#include "Stage.h"

using namespace NativeFaceDetector;

Stage::Stage(float threshold)
	: m_threshold(threshold)
{
}

void Stage::setScale(float scale)
{
	for (std::vector<Tree>::iterator i = m_trees.begin(); i < m_trees.end(); ++i)
		i->setScale(scale);
}

bool Stage::pass(const Matrix &grayImage, const Matrix &squares, int i, int j) const
{
	float sum = 0.0f;
	for (std::vector<Tree>::const_iterator it = m_trees.cbegin(); it < m_trees.cend(); ++it)
		sum += it->getVal(grayImage, squares, i, j);

	return sum > m_threshold;
}